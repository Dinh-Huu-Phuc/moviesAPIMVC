using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Movie_API.Repositories;

namespace Movie_API.CustomActionFilter
{
    public class ValidateActorCanDeleteAttribute : ActionFilterAttribute
    {
        private readonly IMovie_ActorRepository _repository; // Đổi repository

        public ValidateActorCanDeleteAttribute(IMovie_ActorRepository repository)
        {
            _repository = repository;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Kiểm tra Id của Actor
            if (context.ActionArguments.TryGetValue("id", out var value) && value is int actorId)
            {
                // Kiểm tra xem ActorId có tồn tại trong bảng Movie_Actors không
                if (_repository.ExistsByActorId(actorId))
                {
                    context.Result = new BadRequestObjectResult(new
                    {
                        message = "Please remove the link in Movie_Actor before deleting this Actor."
                    });
                }
            }

            base.OnActionExecuting(context);
        }
    }
}
