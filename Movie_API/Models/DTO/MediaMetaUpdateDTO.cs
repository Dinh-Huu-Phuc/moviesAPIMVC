namespace Movie_API.Models.DTO
{
    public class MediaMetaUpdateDTO
    {
        public string? Title { get; set; }
        public string? Intro { get; set; }
        public string? Genre { get; set; }
        public int? Year { get; set; }
        public int? MovieId { get; set; }
    }
}
