using Movie_API.Models.Domain;
using Movie_API.Models.DTO;

namespace Movie_API.Repositories
{
    public interface IActorRepository
    {
        List<ActorDTO> GetAllActors();
        ActorNoIdDTO GetActorById(int id);
        AddActorRequestDTO AddActor(AddActorRequestDTO addActorRequestDTO);
        ActorNoIdDTO UpdateActorById(int id, ActorNoIdDTO actorNoIdDTO);
        Actors? DeleteActorById(int id);
        bool HasAnyMovie(int actorId); // Kiểm tra diễn viên có đóng phim nào không
    }
}
