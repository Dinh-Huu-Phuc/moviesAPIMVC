using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Movie_API.Models.DTO;
using Movie_API.Repositories;

namespace Movie_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StudioController : ControllerBase
    {
        private readonly IStudioRepository _studioRepository; // Đổi repository

        public StudioController(IStudioRepository studioRepository)
        {
            _studioRepository = studioRepository;
        }

        [HttpGet("get-all-studios")]
        public IActionResult GetAll()
        {
            var allStudios = _studioRepository.GetAllStudios();
            return Ok(allStudios);
        }

        [HttpGet("get-studio-by-id/{id}")]
        public IActionResult GetStudioById([FromRoute] int id)
        {
            var studioDTO = _studioRepository.GetStudioById(id);
            if (studioDTO == null) return NotFound();
            return Ok(studioDTO);
        }

        [HttpPost("add-studio")]
        public IActionResult AddStudio([FromBody] AddStudioRequestDTO addStudioRequestDTO)
        {
            if (_studioRepository.ExistsByName(addStudioRequestDTO.Name))
            {
                ModelState.AddModelError(nameof(addStudioRequestDTO.Name), "Studio name already exists.");
                return BadRequest(ModelState);
            }

            var studioAdd = _studioRepository.AddStudio(addStudioRequestDTO);
            return Ok(studioAdd);
        }

        [HttpPut("update-studio-by-id/{id}")]
        public IActionResult UpdateStudioById(int id, [FromBody] StudioNoIdDTO studioNoIdDTO)
        {
            var updatedStudio = _studioRepository.UpdateStudioById(id, studioNoIdDTO);
            return Ok(updatedStudio);
        }

        [HttpDelete("delete-studio-by-id/{id}")]
        public IActionResult DeleteStudioById(int id)
        {
            // Kiểm tra xem studio có phim nào không
            if (_studioRepository.HasMovies(id))
            {
                return BadRequest(new
                {
                    error = "Cannot delete Studio because it has related Movies.",
                });
            }

            var deletedStudio = _studioRepository.DeleteStudioById(id);
            if (deletedStudio == null) return NotFound();
            return Ok(deletedStudio);
        }
    }
}
