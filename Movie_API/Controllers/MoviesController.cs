using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Movie_API.CustomActionFilter;
using Movie_API.Data;
using Movie_API.Models.DTO;
using Movie_API.Repositories;

namespace Movie_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MoviesController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly IMovieRepository _movieRepository; // Đổi repository
        private readonly IStudioRepository _studioRepository; // Đổi repository

        private const int MAX_MOVIES_PER_ACTOR = 20;
        private const int MAX_MOVIES_PER_STUDIO_PER_YEAR = 100;

        public MoviesController(AppDbContext dbContext, IMovieRepository movieRepository, IStudioRepository studioRepository)
        {
            _dbContext = dbContext;
            _movieRepository = movieRepository;
            _studioRepository = studioRepository;
        }

        [HttpGet("get-all-movies")]
        public IActionResult GetAll([FromQuery] string? filterOn, [FromQuery] string? filterQuery,
            [FromQuery] string? sortBy, [FromQuery] bool isAscending,
            [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 100)
        {
            var allMovies = _movieRepository.GetAllMovies(filterOn, filterQuery, sortBy, isAscending, pageNumber, pageSize);
            return Ok(allMovies);
        }

        [HttpGet("get-movie-by-id/{id}")]
        public IActionResult GetMovieById([FromRoute] int id)
        {
            var movieWithIdDTO = _movieRepository.GetMovieById(id);
            return Ok(movieWithIdDTO);
        }

        [HttpPost("add-movie")]
        [ValidateModel]
        [ServiceFilter(typeof(ValidateStudioExistsAttribute))] // Đổi Action Filter
        public IActionResult AddMovie([FromBody] AddMovieRequestDTO addMovieRequestDTO)
        {
            if (ValidateAddMovie(addMovieRequestDTO)) // Đổi hàm validate
            {
                var movieAdd = _movieRepository.AddMovie(addMovieRequestDTO);
                return Ok(movieAdd);
            }
            else return BadRequest(ModelState);
        }

        [HttpPut("update-movie-by-id/{id}")]
        public IActionResult UpdateMovieById(int id, [FromBody] AddMovieRequestDTO movieDTO)
        {
            try
            {
                var updateMovie = _movieRepository.UpdateMovieById(id, movieDTO);
                return Ok(updateMovie);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("delete-movie-by-id/{id}")]
        public IActionResult DeleteMovieById(int id)
        {
            var deleteMovie = _movieRepository.DeleteMovieById(id);
            return Ok(deleteMovie);
        }

        private bool ValidateAddMovie(AddMovieRequestDTO addMovieRequestDTO) // Đổi hàm validate
        {
            if (addMovieRequestDTO == null)
            {
                ModelState.AddModelError(nameof(addMovieRequestDTO), "Please add movie data");
                return false;
            }

            if (addMovieRequestDTO.Rating < 0 || addMovieRequestDTO.Rating > 5)
            {
                ModelState.AddModelError(nameof(addMovieRequestDTO.Rating), $"{nameof(addMovieRequestDTO.Rating)} cannot be less than 0 and more than 5");
            }

            // Kiểm tra StudioID (thay cho PublisherID)
            if (!_studioRepository.ExistsById(addMovieRequestDTO.StudioID))
            {
                ModelState.AddModelError(nameof(addMovieRequestDTO.StudioID), $"{nameof(addMovieRequestDTO.StudioID)} does not exist in Studios table");
            }

            // Kiểm tra ActorIds (thay cho AuthorIds)
            if (addMovieRequestDTO.ActorIds == null || !addMovieRequestDTO.ActorIds.Any())
            {
                ModelState.AddModelError(nameof(addMovieRequestDTO.ActorIds), "A movie must have at least one actor.");
            }
            else
            {
                foreach (var actorId in addMovieRequestDTO.ActorIds)
                {
                    if (!_dbContext.Actors.Any(a => a.Id == actorId))
                    {
                        ModelState.AddModelError(nameof(addMovieRequestDTO.ActorIds), $"Actor with ID {actorId} does not exist.");
                    }
                }
            }

            // check số lượng tối đa một diễn viên đóng phim
            foreach (var actorId in addMovieRequestDTO.ActorIds)
            {
                int currentCount = _dbContext.Movie_Actors.Count(ma => ma.ActorId == actorId);
                if (currentCount >= MAX_MOVIES_PER_ACTOR)
                {
                    ModelState.AddModelError(nameof(addMovieRequestDTO.ActorIds), $"Actor with ID {actorId} already has {currentCount} movies. Maximum allowed is {MAX_MOVIES_PER_ACTOR}.");
                }
            }

            // check số lượng tối đa một năm hãng phim có thể sản xuất
            int year = addMovieRequestDTO.DateAdded.Year;
            int publishedCount = _dbContext.Movies.Count(m =>
                m.StudioID == addMovieRequestDTO.StudioID &&
                m.DateAdded.Year == year);
            if (publishedCount >= MAX_MOVIES_PER_STUDIO_PER_YEAR)
            {
                ModelState.AddModelError(nameof(addMovieRequestDTO.StudioID), $"Studio with ID {addMovieRequestDTO.StudioID} already published {publishedCount} movies in {year}. Maximum allowed is {MAX_MOVIES_PER_STUDIO_PER_YEAR}.");
            }

            // Title không trùng trong cùng 1 Studio
            bool duplicateTitle = _dbContext.Movies.Any(m => m.StudioID == addMovieRequestDTO.StudioID && m.Title.ToLower().Trim() == addMovieRequestDTO.Title.ToLower().Trim());
            if (duplicateTitle)
            {
                ModelState.AddModelError(nameof(addMovieRequestDTO.Title), $"The title '{addMovieRequestDTO.Title}' already exists for this Studio");
            }

            if (ModelState.ErrorCount > 0)
            {
                return false;
            }
            return true;
        }
    }
}
