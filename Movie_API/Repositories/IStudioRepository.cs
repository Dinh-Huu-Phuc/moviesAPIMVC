using Movie_API.Models.Domain;
using Movie_API.Models.DTO;

namespace Movie_API.Repositories
{
    public interface IStudioRepository
    {
        List<StudioDTO> GetAllStudios();
        StudioNoIdDTO GetStudioById(int id);
        AddStudioRequestDTO AddStudio(AddStudioRequestDTO addStudioRequestDTO);
        StudioNoIdDTO UpdateStudioById(int id, StudioNoIdDTO studioNoIdDTO);
        Studios? DeleteStudioById(int id);
        bool ExistsByName(string name);
        bool ExistsById(int id);
        bool ExistsByNameExcludingId(string name, int excludeId);
        bool HasMovies(int studioId); // Kiểm tra hãng phim có phim nào không
    }
}
