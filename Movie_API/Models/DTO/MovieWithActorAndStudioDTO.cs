namespace Movie_API.Models.DTO
{
    public class MovieWithActorAndStudioDTO
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public bool IsWatched { get; set; } 
        public DateTime? DateWatched { get; set; } 
        public int? Rating { get; set; } 
        public string? Genre { get; set; }
        public string? PosterUrl { get; set; }
        public DateTime DateAdded { get; set; }
        public string StudioName { get; set; } 
        public List<string> ActorNames { get; set; } 
    }
}
