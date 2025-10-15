using Movie_API.Models.Domain;
using Movie_API.Models.DTO;

namespace Movie_API.Repositories
{
    public interface IMovieRepository
    {
        List<MovieWithActorAndStudioDTO> GetAllMovies(string? filterOn = null, string? filterQuery = null, string? sortBy = null, bool isAscending = true, int pageNumber = 1, int pageSize = 1000);
        MovieWithActorAndStudioDTO GetMovieById(int id);
        AddMovieRequestDTO AddMovie(AddMovieRequestDTO addMovieRequestDTO);
        AddMovieRequestDTO? UpdateMovieById(int id, AddMovieRequestDTO movieDTO);
        Movies? DeleteMovieById(int id);
    }
}
