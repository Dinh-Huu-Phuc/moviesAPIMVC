using Movie_API.Data;
using Movie_API.Models.Domain;
using System.IO; // Thêm using này cho Path và Directory

namespace Movie_API.Repositories
{
    public class LocalImageRepository : IImageRepository
    {
        private readonly AppDbContext _db;

        // Constructor giờ chỉ cần AppDbContext
        public LocalImageRepository(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Chỉ thêm record vào database. Việc lưu file vật lý đã được Controller xử lý.
        /// </summary>
        public void Upload(Image image)
        {
            _db.Images.Add(image);
            _db.SaveChanges();
        }

        /// <summary>
        /// Lấy tất cả thông tin file từ database.
        /// </summary>
        public IEnumerable<Image> GetAllInfoImages()
            => _db.Images.AsEnumerable();

        /// <summary>
        /// Đọc file vật lý từ thư mục 'uploads' và trả về dưới dạng byte array.
        /// </summary>
        public (byte[]?, string, string) DownloadFile(int id)
        {
            var img = _db.Images.FirstOrDefault(x => x.Id == id);
            if (img == null) return (null, "", "");

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", img.FileName);

            // Kiểm tra file có tồn tại trước khi đọc
            if (!System.IO.File.Exists(filePath)) return (null, "", "");

            var bytes = System.IO.File.ReadAllBytes(filePath);
            var downloadName = img.FileName;

            // Dùng switch expression cho gọn gàng
            var contentType = img.FileExtension switch
            {
                ".mp4" => "video/mp4",
                ".mov" => "video/quicktime",
                ".webm" => "video/webm",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                _ => "application/octet-stream"
            };
            return (bytes, downloadName, contentType);
        }

        // --- CÁC PHƯƠNG THỨC CRUD ĐƯỢC BỔ SUNG ---

        public Image? GetById(int id) => _db.Images.FirstOrDefault(x => x.Id == id);

        public void Update(Image image)
        {
            _db.Images.Update(image);
            _db.SaveChanges();
        }

        public bool Delete(int id)
        {
            var img = _db.Images.FirstOrDefault(x => x.Id == id);
            if (img == null) return false;

            _db.Images.Remove(img);
            _db.SaveChanges();

      

            return true;
        }
    }
}