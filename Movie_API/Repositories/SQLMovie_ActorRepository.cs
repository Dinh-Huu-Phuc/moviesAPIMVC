using Movie_API.Data;
using Movie_API.Models.Domain;
using Movie_API.Models.DTO;
using System;

namespace Movie_API.Repositories
{
    public class SQLMovie_ActorRepository : IMovie_ActorRepository
    {
        private readonly AppDbContext _dbContext;
        public SQLMovie_ActorRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public AddMovie_ActorRequestDTO AddMovie_Actor(AddMovie_ActorRequestDTO addMovie_ActorRequestDTO)
        {
            var movieActorDomain = new Movie_Actors
            {
                MovieId = addMovie_ActorRequestDTO.MovieId,
                ActorId = addMovie_ActorRequestDTO.ActorId,
            };
            _dbContext.Movie_Actors.Add(movieActorDomain);
            _dbContext.SaveChanges();
            return addMovie_ActorRequestDTO;
        }

        public bool ExistsByMovieId(int movieId)
        {
            return _dbContext.Movies.Any(b => b.Id == movieId);
        }

        public bool ExistsByActorId(int actorId)
        {
            return _dbContext.Actors.Any(a => a.Id == actorId);
        }

        public bool Exists(int movieId, int actorId)
        {
            return _dbContext.Movie_Actors.Any(ma => ma.MovieId == movieId && ma.ActorId == actorId);
        }
    }
}
