using System.ComponentModel.DataAnnotations;

namespace Movie_API.Models.DTO
{
    public class AddMovieRequestDTO
    {
        [Required]
        [MinLength(5)]
        public string? Title { get; set; }
        public string? Description { get; set; }
        public bool IsWatched { get; set; } 
        public DateTime? DateWatched { get; set; } 
        public int? Rating { get; set; } 
        public string? Genre { get; set; }
        public string? PosterUrl { get; set; } 
        public DateTime DateAdded { get; set; }

        // Navigation Properties
        public int StudioID { get; set; } 
        public List<int> ActorIds { get; set; } 
    }
}
