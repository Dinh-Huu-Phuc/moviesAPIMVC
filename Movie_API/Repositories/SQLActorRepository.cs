using Movie_API.Data;
using Movie_API.Models.Domain;
using Movie_API.Models.DTO;
using System;

namespace Movie_API.Repositories
{
    public class SQLActorRepository : IActorRepository
    {
        private readonly AppDbContext _dbContext;
        public SQLActorRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<ActorDTO> GetAllActors()
        {
            // Lấy dữ liệu từ bảng Actors
            var allActors = _dbContext.Actors.Select(actor => new ActorDTO
            {
                Id = actor.Id,
                FullName = actor.FullName
            }).ToList();
            return allActors;
        }

        public ActorNoIdDTO GetActorById(int id)
        {
            var actorWithDomain = _dbContext.Actors.Where(n => n.Id == id);
            var actorWithIdDTO = actorWithDomain.Select(actor => new ActorNoIdDTO
            {
                FullName = actor.FullName
            }).FirstOrDefault();
            return actorWithIdDTO;
        }

        public AddActorRequestDTO AddActor(AddActorRequestDTO addActorRequestDTO)
        {
            var actorDomain = new Actors // Tạo đối tượng Actors
            {
                FullName = addActorRequestDTO.FullName
            };
            _dbContext.Actors.Add(actorDomain); // Thêm vào Actors table
            _dbContext.SaveChanges();
            return addActorRequestDTO;
        }

        public ActorNoIdDTO UpdateActorById(int id, ActorNoIdDTO actorNoIdDTO)
        {
            var actorDomain = _dbContext.Actors.FirstOrDefault(n => n.Id == id);
            if (actorDomain != null)
            {
                actorDomain.FullName = actorNoIdDTO.FullName;
                _dbContext.SaveChanges();
            }
            return actorNoIdDTO;
        }

        public Actors? DeleteActorById(int id)
        {
            var actorDomain = _dbContext.Actors.FirstOrDefault(n => n.Id == id);
            if (actorDomain != null)
            {
                _dbContext.Actors.Remove(actorDomain);
                _dbContext.SaveChanges();
                return actorDomain;
            }
            return null;
        }

        // Kiểm tra xem diễn viên có tham gia phim nào không
        public bool HasAnyMovie(int actorId)
        {
            return _dbContext.Movie_Actors.Any(ma => ma.ActorId == actorId); // Dùng bảng Movie_Actors
        }
    }
}
