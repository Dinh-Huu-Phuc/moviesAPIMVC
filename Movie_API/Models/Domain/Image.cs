using System.ComponentModel.DataAnnotations.Schema;

namespace Movie_API.Models.Domain
{
    public class Image
    {

        public int Id { get; set; }

        [NotMapped]
        public IFormFile File { get; set; }

        public string FileName { get; set; }

        public string? FileDescription { get; set; }

        public string FileExtension { get; set; }

        public long FileSizeInBytes { get; set; }

        
        public string? FilePath { get; set; } 

        public string? ThumbnailFileName { get; set; }

        public string? Title { get; set; }
        public string? Intro { get; set; }
        public string? Genre { get; set; } // Sửa lại từ "Genrne"
        public int? Year { get; set; }
        public int? MovieId { get; set; }
    }
}
