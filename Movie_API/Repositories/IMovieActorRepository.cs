using Movie_API.Models.Domain;
using Movie_API.Models.DTO;

namespace Movie_API.Repositories
{
    public interface IMovieActorRepository
    {
        bool MovieExists(int movieId);
        bool ActorExists(int actorId);
        bool RelationExists(int movieId, int actorId);
        Movie_Actors AddRelation(AddMovie_ActorRequestDTO dto);
    }
}
