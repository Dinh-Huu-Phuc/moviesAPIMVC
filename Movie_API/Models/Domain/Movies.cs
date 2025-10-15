using System.ComponentModel.DataAnnotations;

namespace Movie_API.Models.Domain
{
    public class Movies
    {
        [Key]
        public int Id { get; set; }

        public string Title { get; set; }
        public string Description { get; set; }
        public bool IsWatched { get; set; } 
        public DateTime? DateWatched { get; set; } 
        public int? Rating { get; set; }
        public string Genre { get; set; }
        public string? PosterUrl { get; set; } 
        public DateTime DateAdded { get; set; }

        // Foreign Key cho Studio (Publisher)
        public int StudioID { get; set; } 
        public Studios Studio { get; set; } 

        // Navigation property cho quan hệ nhiều-nhiều
        public List<Movie_Actors> Movie_Actor { get; set; }
    }
}
