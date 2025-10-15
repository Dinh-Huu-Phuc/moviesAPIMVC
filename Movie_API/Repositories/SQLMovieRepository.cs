using Movie_API.Data;
using Movie_API.Models.Domain;
using Movie_API.Models.DTO;
using System;

namespace Movie_API.Repositories
{
    public class SQLMovieRepository : IMovieRepository
    {
        private readonly AppDbContext _dbContext;
        public SQLMovieRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<MovieWithActorAndStudioDTO> GetAllMovies(string? filterOn = null, string? filterQuery = null, string? sortBy = null, bool isAscending = true, int pageNumber = 1, int pageSize = 1000)
        {
            var allMovies = _dbContext.Movies.Select(movie => new MovieWithActorAndStudioDTO()
            {
                Id = movie.Id,
                Title = movie.Title,
                Description = movie.Description,
                IsWatched = movie.IsWatched, // IsWatched
                DateWatched = movie.IsWatched ? movie.DateWatched.Value : null, // DateWatched
                Rating = movie.IsWatched ? movie.Rating.Value : null, // Rating
                Genre = movie.Genre,
                PosterUrl = movie.PosterUrl, // PosterUrl
                StudioName = movie.Studio.Name, // Studio
                ActorNames = movie.Movie_Actor.Select(n => n.Actor.FullName).ToList() // Movie_Actor
            }).AsQueryable();

            // filtering
            if (string.IsNullOrWhiteSpace(filterOn) == false && string.IsNullOrWhiteSpace(filterQuery) == false)
            {
                if (filterOn.Equals("title", StringComparison.OrdinalIgnoreCase))
                {
                    allMovies = allMovies.Where(x => x.Title.Contains(filterQuery));
                }
            }

            // sorting
            if (string.IsNullOrWhiteSpace(sortBy) == false)
            {
                if (sortBy.Equals("title", StringComparison.OrdinalIgnoreCase))
                {
                    allMovies = isAscending ? allMovies.OrderBy(x => x.Title) : allMovies.OrderByDescending(x => x.Title);
                }
            }

            // pagination
            var skipResults = (pageNumber - 1) * pageSize;
            return allMovies.Skip(skipResults).Take(pageSize).ToList();
        }

        public MovieWithActorAndStudioDTO GetMovieById(int id)
        {
            var movieWithDomain = _dbContext.Movies.Where(n => n.Id == id);
            var movieWithIdDTO = movieWithDomain.Select(movie => new MovieWithActorAndStudioDTO()
            {
                Id = movie.Id,
                Title = movie.Title,
                Description = movie.Description,
                IsWatched = movie.IsWatched,
                DateWatched = movie.DateWatched,
                Rating = movie.Rating,
                Genre = movie.Genre,
                PosterUrl = movie.PosterUrl,
                StudioName = movie.Studio.Name,
                ActorNames = movie.Movie_Actor.Select(n => n.Actor.FullName).ToList()
            }).FirstOrDefault();
            return movieWithIdDTO;
        }

        public AddMovieRequestDTO AddMovie(AddMovieRequestDTO addMovieRequestDTO)
        {
            var movieDomainModel = new Movies
            {
                Title = addMovieRequestDTO.Title,
                Description = addMovieRequestDTO.Description,
                IsWatched = addMovieRequestDTO.IsWatched,
                DateWatched = addMovieRequestDTO.DateWatched,
                Rating = addMovieRequestDTO.Rating,
                Genre = addMovieRequestDTO.Genre,
                PosterUrl = addMovieRequestDTO.PosterUrl,
                DateAdded = addMovieRequestDTO.DateAdded,
                StudioID = addMovieRequestDTO.StudioID // StudioID
            };
            _dbContext.Movies.Add(movieDomainModel);
            _dbContext.SaveChanges();

            foreach (var id in addMovieRequestDTO.ActorIds) // ActorIds
            {
                var movieActor = new Movie_Actors()
                {
                    MovieId = movieDomainModel.Id,
                    ActorId = id
                };
                _dbContext.Movie_Actors.Add(movieActor);
            }
            _dbContext.SaveChanges();
            return addMovieRequestDTO;
        }

        public AddMovieRequestDTO? UpdateMovieById(int id, AddMovieRequestDTO movieDTO)
        {
            var movieDomain = _dbContext.Movies.FirstOrDefault(n => n.Id == id);
            if (movieDomain == null) throw new Exception($"Movie with ID {id} does not exist.");

            var studioExists = _dbContext.Studios.Any(p => p.Id == movieDTO.StudioID);
            if (!studioExists) throw new Exception($"Studio with ID {movieDTO.StudioID} does not exist.");

            foreach (var actorId in movieDTO.ActorIds)
            {
                var actorExists = _dbContext.Actors.Any(a => a.Id == actorId);
                if (!actorExists) throw new Exception($"Actor with ID {actorId} does not exist.");
            }

            movieDomain.Title = movieDTO.Title;
            movieDomain.Description = movieDTO.Description;
            movieDomain.IsWatched = movieDTO.IsWatched;
            movieDomain.DateWatched = movieDTO.DateWatched;
            movieDomain.Rating = movieDTO.Rating;
            movieDomain.Genre = movieDTO.Genre;
            movieDomain.PosterUrl = movieDTO.PosterUrl;
            movieDomain.DateAdded = movieDTO.DateAdded;
            movieDomain.StudioID = movieDTO.StudioID;
            _dbContext.SaveChanges();

            var oldActors = _dbContext.Movie_Actors.Where(a => a.MovieId == id).ToList();
            _dbContext.Movie_Actors.RemoveRange(oldActors);

            foreach (var actorId in movieDTO.ActorIds)
            {
                var movieActor = new Movie_Actors()
                {
                    MovieId = id,
                    ActorId = actorId
                };
                _dbContext.Movie_Actors.Add(movieActor);
            }
            _dbContext.SaveChanges();
            return movieDTO;
        }

        public Movies? DeleteMovieById(int id)
        {
            var movieDomain = _dbContext.Movies.FirstOrDefault(n => n.Id == id);
            if (movieDomain != null)
            {
                _dbContext.Movies.Remove(movieDomain);
                _dbContext.SaveChanges();
            }
            return movieDomain;
        }
    }
}
