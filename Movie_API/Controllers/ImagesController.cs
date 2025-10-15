using System.IO;
using Microsoft.AspNetCore.Mvc;
using Movie_API.Repositories;
using Movie_API.Models.Domain;
using Movie_API.Models.DTO;

namespace Movie_API.Controllers
{
    [ApiController]
    [Route("api/Movie")]
    [Tags("Movie Files")]
    public class ImagesController : ControllerBase
    {
        private readonly IImageRepository _imageRepository;
        private readonly IWebHostEnvironment _env;

        public ImagesController(IImageRepository imageRepository, IWebHostEnvironment env)
        {
            _imageRepository = imageRepository;
            _env = env;
        }

        // POST /api/Movie/Upload (tối đa 500MB)
        [HttpPost]
        [Route("Upload")]
        [RequestSizeLimit(500L * 1024 * 1024)]
        public async Task<IActionResult> Upload([FromForm] ImageUploadRequestDTO request)
        {
            ValidateFileUpload(request);
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var uploadsDir = Path.Combine(_env.ContentRootPath, "uploads");
            Directory.CreateDirectory(uploadsDir);

            var ext = Path.GetExtension(request.File.FileName).ToLowerInvariant();
            var storedName = $"{Guid.NewGuid()}{ext}";
            var savePath = Path.Combine(uploadsDir, storedName);

            await using (var fs = System.IO.File.Create(savePath))
            {
                await request.File.CopyToAsync(fs);
            }

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var fileUrl = $"{baseUrl}/uploads/{storedName}";

            var image = new Image
            {
                FileName = storedName,
                FileExtension = ext,
                FileSizeInBytes = request.File.Length,
                FileDescription = request.FileDescription,
                FilePath = storedName
            };
            _imageRepository.Upload(image);

            return Ok(new { id = image.Id, fileName = storedName, fileUrl });
        }

        // GET /api/Movie/GetAllImages
        [HttpGet]
        [Route("GetAllImages")]
        public IActionResult GetInfoAllImages()
        {
            var list = _imageRepository.GetAllInfoImages();
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            // ✅ LOGIC ĐÃ ĐƯỢC CẬP NHẬT
            var shaped = list.Select(x => new
            {
                id = x.Id,
                fileName = x.FileName,                  // đã có đuôi sẵn
                fileDescription = x.FileDescription,
                fileExtension = x.FileExtension,
                fileSizeInBytes = x.FileSizeInBytes,
                fileUrl = $"{baseUrl}/uploads/{x.FileName}"  // không tự ghép thêm gì nữa
            });

            return Ok(shaped);
        }

        // GET /api/Movie/Download?id=...
        [HttpGet]
        [Route("Download")]
        public IActionResult DownloadImage(int id)
        {
            var result = _imageRepository.DownloadFile(id);
            if (result.Item1 == null) return NotFound("File not found");
            return File(result.Item1, result.Item3, result.Item2);
        }

        private void ValidateFileUpload(ImageUploadRequestDTO request)
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".mp4", ".webm", ".mov" };
            var ext = Path.GetExtension(request.File.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
                ModelState.AddModelError("file", "Unsupported file extension");

            const long limit = 500L * 1024 * 1024; // 500MB
            if (request.File.Length > limit)
                ModelState.AddModelError("file", "File size more than 500MB.");
        }

        [HttpPut("Update/{id:int}")]
        [RequestSizeLimit(500L * 1024 * 1024)]
        public async Task<IActionResult> UpdateAsync(int id, [FromForm] ImageUpdateRequestDTO req)
        {
            var item = _imageRepository.GetById(id);
            if (item == null) return NotFound("Image not found");

            // Nếu có file mới -> validate + thay thế vật lý
            if (req.File != null && req.File.Length > 0)
            {
                var ext = Path.GetExtension(req.File.FileName).ToLowerInvariant();
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".mp4", ".webm", ".mov" };
                if (!allowed.Contains(ext))
                    return BadRequest("Unsupported file extension");

                const long limit = 500L * 1024 * 1024;
                if (req.File.Length > limit)
                    return BadRequest("File size more than 500MB.");

                var uploadsDir = Path.Combine(_env.ContentRootPath, "uploads");
                Directory.CreateDirectory(uploadsDir);

                // xóa file cũ (nếu có)
                var oldFullPath = Path.Combine(uploadsDir, item.FileName);
                if (System.IO.File.Exists(oldFullPath))
                {
                    try { System.IO.File.Delete(oldFullPath); } catch { /* ignore */ }
                }

                // lưu file mới
                var newStoredName = $"{Guid.NewGuid()}{ext}";
                var newFullPath = Path.Combine(uploadsDir, newStoredName);
                await using (var fs = System.IO.File.Create(newFullPath))
                {
                    await req.File.CopyToAsync(fs);
                }

                // cập nhật thông tin DB
                item.FileName = newStoredName;
                item.FileExtension = ext;
                item.FileSizeInBytes = req.File.Length;
                item.FilePath = newStoredName;
            }

            // Cập nhật mô tả (nếu gửi)
            if (!string.IsNullOrWhiteSpace(req.FileDescription))
            {
                item.FileDescription = req.FileDescription;
            }

            _imageRepository.Update(item);

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var fileUrl = $"{baseUrl}/uploads/{item.FileName}";
            return Ok(new
            {
                id = item.Id,
                fileName = item.FileName,
                fileExtension = item.FileExtension,
                fileSizeInBytes = item.FileSizeInBytes,
                fileDescription = item.FileDescription,
                fileUrl
            });
        }

        // ================== DELETE (xóa DB + file vật lý) ==================
        // DELETE: /api/Movie/Delete/{id}
        [HttpDelete("Delete/{id:int}")]
        public IActionResult Delete(int id)
        {
            var item = _imageRepository.GetById(id);
            if (item == null) return NotFound("Image not found");

            // xóa DB record
            var ok = _imageRepository.Delete(id);
            if (!ok) return StatusCode(500, "Delete DB failed");

            // xóa file vật lý
            var uploadsDir = Path.Combine(_env.ContentRootPath, "uploads");
            var full = Path.Combine(uploadsDir, item.FileName);
            if (System.IO.File.Exists(full))
            {
                try { System.IO.File.Delete(full); } catch { /* ignore */ }
            }

            return NoContent();
        }
    }

}
