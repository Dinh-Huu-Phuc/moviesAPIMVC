using Movie_API.Data;
using Movie_API.Models.Domain;
using Movie_API.Models.DTO;
using System;

namespace Movie_API.Repositories
{
    public class SQLMovieActorRepository : IMovieActorRepository
    {
        private readonly AppDbContext _context;
        public SQLMovieActorRepository(AppDbContext context) => _context = context;

        public bool MovieExists(int movieId) => _context.Movies.Any(m => m.Id == movieId);
        public bool ActorExists(int actorId) => _context.Actors.Any(a => a.Id == actorId);

        public bool RelationExists(int movieId, int actorId)
            => _context.Movie_Actors.Any(x => x.MovieId == movieId && x.ActorId == actorId);

        public Movie_Actors AddRelation(AddMovie_ActorRequestDTO dto)
        {
            var entity = new Movie_Actors { MovieId = dto.MovieId, ActorId = dto.ActorId };
            _context.Movie_Actors.Add(entity);
            _context.SaveChanges();
            return entity;
        }
    }
}
