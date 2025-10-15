using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Movie_API.Models.DTO;
using Movie_API.Repositories;

namespace Movie_API.CustomActionFilter
{
    public class ValidateMovieActorNotExistsAttribute : ActionFilterAttribute
    {
        private readonly IMovie_ActorRepository _repository; // Đổi repository

        public ValidateMovieActorNotExistsAttribute(IMovie_ActorRepository repository)
        {
            _repository = repository;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.ActionArguments.TryGetValue("addMovie_ActorRequestDTO", out var value) && value is AddMovie_ActorRequestDTO dto)
            {
                if (_repository.Exists(dto.MovieId, dto.ActorId))
                {
                    context.Result = new ConflictObjectResult(new
                    {
                        message = $"The relationship MovieID={dto.MovieId} and ActorID={dto.ActorId} already exists."
                    });
                }
            }
            base.OnActionExecuting(context);
        }
    }
}
