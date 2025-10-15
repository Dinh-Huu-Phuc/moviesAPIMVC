using System.IO;
using Microsoft.AspNetCore.Mvc;
using Movie_API.Repositories;
using Movie_API.Models.Domain;
using Movie_API.Models.DTO;
using Movie_API.Services;

namespace Movie_API.Controllers
{
    [ApiController]
    [Route("api/Movie")]
    [Tags("Movie Files")]
    public class ImagesController : ControllerBase
    {
        private readonly IImageRepository _imageRepository;
        private readonly IWebHostEnvironment _env;
        private readonly IThumbnailService _thumb;

        public ImagesController(
            IImageRepository imageRepository,
            IWebHostEnvironment env,
            IThumbnailService thumb)
        {
            _imageRepository = imageRepository;
            _env = env;
            _thumb = thumb;
        }

        // ======================= CREATE (Upload video/image) =======================
        // POST: /api/Movie/Upload
        [HttpPost("Upload")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(500L * 1024 * 1024)] // 500MB
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
                await request.File.CopyToAsync(fs);

            // nếu là video thì auto tạo thumbnail
            string? thumbName = null;
            if (ext is ".mp4" or ".mov" or ".m4v" or ".webm")
            {
                try { thumbName = await _thumb.CreateFromVideoAsync(savePath, uploadsDir); }
                catch { /* best effort */ }
            }

            var image = new Image
            {
                FileName = storedName,
                FileExtension = ext,
                FileSizeInBytes = request.File.Length,
                FileDescription = request.FileDescription,
                FilePath = storedName,
                ThumbnailFileName = thumbName
            };
            _imageRepository.Upload(image);

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var fileUrl = $"{baseUrl}/uploads/{storedName}";
            var thumbUrl = thumbName != null ? $"{baseUrl}/uploads/{thumbName}" : null;

            return Ok(new
            {
                id = image.Id,
                fileName = storedName,
                fileUrl,
                thumbnailUrl = thumbUrl
            });
        }

        // ======================= READ (List) =======================
        // GET: /api/Movie/GetAllImages
        [HttpGet("GetAllImages")]
        public IActionResult GetInfoAllImages()
        {
            var list = _imageRepository.GetAllInfoImages();
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var shaped = list.Select(x => new
            {
                id = x.Id,
                fileName = x.FileName,
                fileDescription = x.FileDescription,
                fileExtension = x.FileExtension,
                fileSizeInBytes = x.FileSizeInBytes,
                fileUrl = $"{baseUrl}/uploads/{x.FileName}",
                thumbnailUrl = !string.IsNullOrEmpty(x.ThumbnailFileName)
                                ? $"{baseUrl}/uploads/{x.ThumbnailFileName}"
                                : null
            });

            return Ok(shaped);
        }

        // GET: /api/Movie/Download?id=...
        [HttpGet("Download")]
        public IActionResult DownloadImage(int id)
        {
            var result = _imageRepository.DownloadFile(id);
            if (result.Item1 == null) return NotFound("File not found");
            return File(result.Item1, result.Item3, result.Item2);
        }

        // ======================= UPDATE (replace file / update desc) =======================
        // PUT: /api/Movie/Update/{id}
        [HttpPut("Update/{id:int}")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(500L * 1024 * 1024)]
        public async Task<IActionResult> UpdateAsync(int id, [FromForm] ImageUpdateRequestDTO req)
        {
            var item = _imageRepository.GetById(id);
            if (item == null) return NotFound("Image not found");

            var uploadsDir = Path.Combine(_env.ContentRootPath, "uploads");
            Directory.CreateDirectory(uploadsDir);

            // nếu upload file mới thì thay thế và tái tạo thumbnail nếu là video
            if (req.File != null && req.File.Length > 0)
            {
                ValidateFileUpload(new ImageUploadRequestDTO { File = req.File });
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var ext = Path.GetExtension(req.File.FileName).ToLowerInvariant();

                // Xoá file cũ
                var oldFullPath = Path.Combine(uploadsDir, item.FileName);
                if (System.IO.File.Exists(oldFullPath))
                {
                    try { System.IO.File.Delete(oldFullPath); } catch { /* ignore */ }
                }
                if (!string.IsNullOrEmpty(item.ThumbnailFileName))
                {
                    var oldThumb = Path.Combine(uploadsDir, item.ThumbnailFileName);
                    if (System.IO.File.Exists(oldThumb))
                    {
                        try { System.IO.File.Delete(oldThumb); } catch { /* ignore */ }
                    }
                }

                // Lưu file mới
                var newStoredName = $"{Guid.NewGuid()}{ext}";
                var newFullPath = Path.Combine(uploadsDir, newStoredName);
                await using (var fs = System.IO.File.Create(newFullPath))
                    await req.File.CopyToAsync(fs);

                // tạo thumbnail nếu là video
                string? newThumb = null;
                if (ext is ".mp4" or ".mov" or ".m4v" or ".webm")
                {
                    try { newThumb = await _thumb.CreateFromVideoAsync(newFullPath, uploadsDir); }
                    catch { /* ignore */ }
                }

                item.FileName = newStoredName;
                item.FileExtension = ext;
                item.FileSizeInBytes = req.File.Length;
                item.ThumbnailFileName = newThumb;
            }

            if (!string.IsNullOrWhiteSpace(req.FileDescription))
                item.FileDescription = req.FileDescription;

            _imageRepository.Update(item);

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            return Ok(new
            {
                id = item.Id,
                fileName = item.FileName,
                fileExtension = item.FileExtension,
                fileSizeInBytes = item.FileSizeInBytes,
                fileDescription = item.FileDescription,
                fileUrl = $"{baseUrl}/uploads/{item.FileName}",
                thumbnailUrl = !string.IsNullOrEmpty(item.ThumbnailFileName)
                                ? $"{baseUrl}/uploads/{item.ThumbnailFileName}"
                                : null
            });
        }

        // ======================= UPDATE THUMBNAIL (CÁCH 1) =======================
        // POST: /api/Movie/{id}/thumbnail
        [HttpPost("{id:int}/thumbnail")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(20L * 1024 * 1024)] // 20MB
        public async Task<IActionResult> UploadThumbnail(int id, [FromForm] ThumbnailUploadRequestDTO req)
        {
            if (req.File == null || req.File.Length == 0)
                return BadRequest("Thiếu file ảnh.");

            var ext = Path.GetExtension(req.File.FileName).ToLowerInvariant();
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            if (!allowed.Contains(ext))
                return BadRequest("Chỉ hỗ trợ .jpg .jpeg .png .gif .webp");

            var item = _imageRepository.GetById(id);
            if (item == null) return NotFound($"Không tìm thấy Image id={id}");

            var uploadsDir = Path.Combine(_env.ContentRootPath, "uploads");
            Directory.CreateDirectory(uploadsDir);

            // Xoá thumbnail cũ (nếu có)
            if (!string.IsNullOrEmpty(item.ThumbnailFileName))
            {
                var oldThumb = Path.Combine(uploadsDir, item.ThumbnailFileName);
                if (System.IO.File.Exists(oldThumb))
                {
                    try { System.IO.File.Delete(oldThumb); } catch { /* ignore */ }
                }
            }

            var storedName = $"{Path.GetFileNameWithoutExtension(req.File.FileName)}_{Guid.NewGuid()}{ext}";
            var savePath = Path.Combine(uploadsDir, storedName);
            await using (var fs = System.IO.File.Create(savePath))
                await req.File.CopyToAsync(fs);

            item.ThumbnailFileName = storedName;
            _imageRepository.Update(item);

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            return Ok(new
            {
                id = item.Id,
                thumbnailFileName = storedName,
                thumbnailUrl = $"{baseUrl}/uploads/{storedName}"
            });
        }


        // ======================= DELETE =======================
        // DELETE: /api/Movie/Delete/{id}
        [HttpDelete("Delete/{id:int}")]
        public IActionResult Delete(int id)
        {
            var item = _imageRepository.GetById(id);
            if (item == null) return NotFound("Image not found");

            var ok = _imageRepository.Delete(id);
            if (!ok) return StatusCode(500, "Could not delete from database.");

            var uploadsDir = Path.Combine(_env.ContentRootPath, "uploads");

            var full = Path.Combine(uploadsDir, item.FileName);
            if (System.IO.File.Exists(full))
            {
                try { System.IO.File.Delete(full); } catch { /* ignore */ }
            }

            if (!string.IsNullOrEmpty(item.ThumbnailFileName))
            {
                var thumb = Path.Combine(uploadsDir, item.ThumbnailFileName);
                if (System.IO.File.Exists(thumb))
                {
                    try { System.IO.File.Delete(thumb); } catch { /* ignore */ }
                }
            }

            return NoContent();
        }

        // ======================= Helpers =======================
        private void ValidateFileUpload(ImageUploadRequestDTO request)
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".mp4", ".webm", ".mov", ".m4v" };
            var ext = Path.GetExtension(request.File.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
                ModelState.AddModelError("file", "Unsupported file extension.");

            const long limit = 500L * 1024 * 1024; // 500MB
            if (request.File.Length > limit)
                ModelState.AddModelError("file", $"File size exceeds the limit of {limit / 1024 / 1024}MB.");
        }
    }
}
