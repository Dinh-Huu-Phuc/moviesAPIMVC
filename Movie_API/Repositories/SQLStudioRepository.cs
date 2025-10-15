using Movie_API.Data;
using Movie_API.Models.Domain;
using Movie_API.Models.DTO;
using System;

namespace Movie_API.Repositories
{
    public class SQLStudioRepository : IStudioRepository
    {
        private readonly AppDbContext _dbContext;
        public SQLStudioRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<StudioDTO> GetAllStudios()
        {
            // Lấy dữ liệu từ bảng Studios
            var allStudios = _dbContext.Studios.Select(studio => new StudioDTO
            {
                Id = studio.Id,
                Name = studio.Name
            }).ToList();
            return allStudios;
        }

        public StudioNoIdDTO GetStudioById(int id)
        {
            var studioWithDomain = _dbContext.Studios.Where(n => n.Id == id);
            var studioWithIdDTO = studioWithDomain.Select(studio => new StudioNoIdDTO
            {
                Name = studio.Name,
            }).FirstOrDefault();
            return studioWithIdDTO;
        }

        public AddStudioRequestDTO AddStudio(AddStudioRequestDTO addStudioRequestDTO)
        {
            var studioDomain = new Studios // Tạo đối tượng Studios
            {
                Name = addStudioRequestDTO.Name
            };
            _dbContext.Studios.Add(studioDomain); // Thêm vào Studios table
            _dbContext.SaveChanges();
            return addStudioRequestDTO;
        }

        public StudioNoIdDTO UpdateStudioById(int id, StudioNoIdDTO studioNoIdDTO)
        {
            var studioDomain = _dbContext.Studios.FirstOrDefault(n => n.Id == id);
            if (studioDomain != null)
            {
                studioDomain.Name = studioNoIdDTO.Name;
                _dbContext.SaveChanges();
            }
            return studioNoIdDTO;
        }

        public Studios? DeleteStudioById(int id)
        {
            var studioDomain = _dbContext.Studios.FirstOrDefault(n => n.Id == id);
            if (studioDomain != null)
            {
                _dbContext.Studios.Remove(studioDomain);
                _dbContext.SaveChanges();
                return studioDomain;
            }
            return null;
        }

        public bool ExistsByName(string name)
        {
            return _dbContext.Studios.Any(p => p.Name.ToLower() == name.ToLower());
        }

        public bool ExistsById(int id)
        {
            return _dbContext.Studios.Any(p => p.Id == id);
        }

        public bool ExistsByNameExcludingId(string name, int excludeId)
        {
            return _dbContext.Studios.Any(p => p.Name.ToLower() == name.ToLower() && p.Id != excludeId);
        }

        // Kiểm tra xem hãng phim có phim nào không
        public bool HasMovies(int studioId)
        {
            return _dbContext.Movies.Any(movie => movie.StudioID == studioId);
        }
    }
}
