using Movie_API.Models.DTO;

namespace Movie_API.Repositories
{
    public interface IMovie_ActorRepository
    {
        AddMovie_ActorRequestDTO AddMovie_Actor(AddMovie_ActorRequestDTO addMovie_ActorRequestDTO);
        bool ExistsByMovieId(int movieId);
        bool ExistsByActorId(int actorId);
        bool Exists(int movieId, int actorId);
    }
}
