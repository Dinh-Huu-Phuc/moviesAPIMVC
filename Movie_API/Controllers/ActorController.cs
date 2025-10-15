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
    public class ActorController : ControllerBase
    {
        private readonly IActorRepository _actorRepository; // Đổi repository

        public ActorController(IActorRepository actorRepository)
        {
            _actorRepository = actorRepository;
        }

        [HttpGet("get-all-actors")]
        public IActionResult GetAll()
        {
            var allActors = _actorRepository.GetAllActors();
            return Ok(allActors);
        }

        [HttpGet("get-actor-by-id/{id}")]
        public IActionResult GetActorById([FromRoute] int id)
        {
            var actorDTO = _actorRepository.GetActorById(id);
            if (actorDTO == null) return NotFound();
            return Ok(actorDTO);
        }

        [HttpPost("add-actor")]
        [ValidateModel]
        public IActionResult AddActor([FromBody] AddActorRequestDTO addActorRequestDTO)
        {
            var actorAdd = _actorRepository.AddActor(addActorRequestDTO);
            return Ok(actorAdd);
        }

        [HttpPut("update-actor-by-id/{id}")]
        public IActionResult UpdateActorById(int id, [FromBody] ActorNoIdDTO actorNoIdDTO)
        {
            var updatedActor = _actorRepository.UpdateActorById(id, actorNoIdDTO);
            if (updatedActor == null) return NotFound();
            return Ok(updatedActor);
        }

        [HttpDelete("delete-actor-by-id/{id}")]
        public IActionResult DeleteActorById(int id)
        {
            // Kiểm tra xem diễn viên có tham gia phim nào không
            if (_actorRepository.HasAnyMovie(id))
            {
                return BadRequest(new
                {
                    error = "Cannot delete Actor because they are linked to one or more Movies.",
                    suggestion = "Please remove the links in Movie_Actor before deleting."
                });
            }

            var deletedActor = _actorRepository.DeleteActorById(id);
            if (deletedActor == null) return NotFound();
            return Ok(deletedActor);
        }
    }
}
