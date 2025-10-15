namespace Movie_API.Models.DTO
{
    public class ImageUpdateRequestDTO
    {
        // Tuỳ chọn: có thể không gửi File -> chỉ cập nhật mô tả
        public IFormFile? File { get; set; }
        public string? FileDescription { get; set; }
    }
}
