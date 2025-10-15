using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Movie_API.CustomActionFilter;
using Movie_API.Models.DTO;
using Movie_API.Repositories;

namespace Movie_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class Movie_ActorController : ControllerBase
    {
        private readonly IMovie_ActorRepository _movieActorRepository; // Đổi repository

        public Movie_ActorController(IMovie_ActorRepository movieActorRepository)
        {
            _movieActorRepository = movieActorRepository;
        }

        [HttpPost("add-movie-actor")]
        [ValidateModel]
        [ServiceFilter(typeof(ValidateMovieActorNotExistsAttribute))] // Đổi Action Filter
        public IActionResult AddMovie_Actor([FromBody] AddMovie_ActorRequestDTO addMovie_ActorRequestDTO)
        {
            if (!ValidateAddMovie_Actor(addMovie_ActorRequestDTO))
            {
                return BadRequest(ModelState);
            }

            var movie_actorAdd = _movieActorRepository.AddMovie_Actor(addMovie_ActorRequestDTO);
            return Ok(movie_actorAdd);
        }

        private bool ValidateAddMovie_Actor(AddMovie_ActorRequestDTO dto)
        {
            if (!_movieActorRepository.ExistsByMovieId(dto.MovieId))
            {
                ModelState.AddModelError(nameof(dto.MovieId), "MovieId does not exist in Movies table");
            }
            if (!_movieActorRepository.ExistsByActorId(dto.ActorId))
            {
                ModelState.AddModelError(nameof(dto.ActorId), "ActorId does not exist in Actors table");
            }

            return ModelState.ErrorCount == 0;
        }
    }
}
