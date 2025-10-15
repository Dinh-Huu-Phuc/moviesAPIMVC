namespace Movie_API.Models.DTO
{
    public class ThumbnailUploadRequestDTO
    {
        public IFormFile File { get; set; } = default!;
    }
}
