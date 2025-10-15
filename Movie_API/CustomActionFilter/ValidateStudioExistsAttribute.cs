using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Movie_API.Models.DTO;
using Movie_API.Repositories;

namespace Movie_API.CustomActionFilter
{
    public class ValidateStudioExistsAttribute : ActionFilterAttribute
    {
        private readonly IStudioRepository _studioRepository; // Đổi repository

        public ValidateStudioExistsAttribute(IStudioRepository studioRepository)
        {
            _studioRepository = studioRepository;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Kiểm tra DTO của movie
            if (context.ActionArguments.ContainsKey("addMovieRequestDTO"))
            {
                var dto = context.ActionArguments["addMovieRequestDTO"] as AddMovieRequestDTO;
                if (dto != null && !_studioRepository.ExistsById(dto.StudioID))
                {
                    context.Result = new BadRequestObjectResult(new
                    {
                        error = $"Studio with ID {dto.StudioID} does not exist."
                    });
                }
            }
        }
    }
}
