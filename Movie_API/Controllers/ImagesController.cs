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
        private static readonly string[] VideoExts = { ".mp4", ".mov", ".m4v", ".webm" };
        private static readonly string[] ImageExts = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

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

        // POST: /api/Movie/Upload  (tối đa 500MB)
        [HttpPost("Upload")]
        [Consumes("multipart/form-data")]
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

            // tạo thumb tự động cho video (best effort)
            string? thumbName = null;
            if (VideoExts.Contains(ext))
            {
                try { thumbName = await _thumb.CreateFromVideoAsync(savePath, uploadsDir); }
                catch { /* ignore thumbnail failure */ }
            }

            // quan trọng: DB bạn có cột NOT NULL FilePath -> lưu bằng storedName
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
            return Ok(new
            {
                id = image.Id,
                fileName = image.FileName,
                fileUrl = $"{baseUrl}/uploads/{image.FileName}",
                thumbnailUrl = thumbName != null ? $"{baseUrl}/uploads/{thumbName}" : null
            });
        }

        // GET: /api/Movie/GetMediaPaged
        // type: all | video | image
        [HttpGet("GetMediaPaged")]
        public IActionResult GetMediaPaged(
            [FromQuery] int? movieId,          // hiện chưa dùng vì model Image chưa có MovieId
            [FromQuery] string? type = "all",
            [FromQuery] string? q = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 24)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 24;

            // Lấy dữ liệu từ repo (IEnumerable) rồi LINQ
            // (nếu repo của bạn trả về IQueryable cũng ok vì biểu thức dưới đây đều dịch được)
            var query = _imageRepository.GetAllInfoImages().AsQueryable();

            // tìm kiếm theo mô tả/tên
            if (!string.IsNullOrWhiteSpace(q))
            {
                var q2 = q.Trim();
                query = query.Where(x => (x.FileDescription ?? "").Contains(q2));
            }

            // lọc theo loại file
            var kind = (type ?? "all").Trim().ToLowerInvariant();
            if (kind == "video")
            {
                query = query.Where(x => VideoExts.Contains((x.FileExtension ?? "").ToLower()));
            }
            else if (kind == "image")
            {
                query = query.Where(x => ImageExts.Contains((x.FileExtension ?? "").ToLower()));
            }

            var totalCount = query.Count();

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var items = query
                .OrderByDescending(x => x.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new
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
                })
                .ToList();

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return Ok(new
            {
                pageNumber,
                pageSize,
                totalCount,
                totalPages,
                items
            });
        }

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

            // nếu up file mới
            if (req.File != null && req.File.Length > 0)
            {
                ValidateFileUpload(new ImageUploadRequestDTO { File = req.File });
                if (!ModelState.IsValid) return BadRequest(ModelState);

                // xóa file cũ (nếu có)
                var oldPath = Path.Combine(uploadsDir, item.FileName);
                if (System.IO.File.Exists(oldPath))
                {
                    try { System.IO.File.Delete(oldPath); } catch { }
                }
                if (!string.IsNullOrEmpty(item.ThumbnailFileName))
                {
                    var oldThumb = Path.Combine(uploadsDir, item.ThumbnailFileName);
                    if (System.IO.File.Exists(oldThumb))
                    {
                        try { System.IO.File.Delete(oldThumb); } catch { }
                    }
                }

                // lưu file mới
                var ext = Path.GetExtension(req.File.FileName).ToLowerInvariant();
                var newStored = $"{Guid.NewGuid()}{ext}";
                var newFull = Path.Combine(uploadsDir, newStored);

                await using (var fs = System.IO.File.Create(newFull))
                {
                    await req.File.CopyToAsync(fs);
                }

                // tạo thumb nếu là video
                string? newThumb = null;
                if (VideoExts.Contains(ext))
                {
                    try { newThumb = await _thumb.CreateFromVideoAsync(newFull, uploadsDir); }
                    catch { }
                }

                item.FileName = newStored;
                item.FilePath = newStored;                 // cập nhật FilePath để không NULL
                item.FileExtension = ext;
                item.FileSizeInBytes = req.File.Length;
                item.ThumbnailFileName = newThumb;
            }

            // cập nhật mô tả nếu có
            if (!string.IsNullOrWhiteSpace(req.FileDescription))
                item.FileDescription = req.FileDescription;

            _imageRepository.Update(item);

            var baseU = $"{Request.Scheme}://{Request.Host}";
            return Ok(new
            {
                id = item.Id,
                fileName = item.FileName,
                fileExtension = item.FileExtension,
                fileSizeInBytes = item.FileSizeInBytes,
                fileDescription = item.FileDescription,
                fileUrl = $"{baseU}/uploads/{item.FileName}",
                thumbnailUrl = !string.IsNullOrEmpty(item.ThumbnailFileName)
                    ? $"{baseU}/uploads/{item.ThumbnailFileName}"
                    : null
            });
        }

        // POST: /api/Movie/{id}/thumbnail  (up thumb rời)
        [HttpPost("{id:int}/thumbnail")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(20L * 1024 * 1024)]
        public async Task<IActionResult> UploadThumbnail(int id, [FromForm] ThumbnailUploadRequestDTO req)
        {
            if (req.File == null || req.File.Length == 0)
                return BadRequest("Thiếu file ảnh.");

            var ext = Path.GetExtension(req.File.FileName).ToLowerInvariant();
            if (!ImageExts.Contains(ext))
                return BadRequest("Chỉ hỗ trợ .jpg .jpeg .png .gif .webp");

            var item = _imageRepository.GetById(id);
            if (item == null) return NotFound($"Không tìm thấy Image id={id}");

            var uploadsDir = Path.Combine(_env.ContentRootPath, "uploads");
            Directory.CreateDirectory(uploadsDir);

            // xóa thumb cũ (nếu có)
            if (!string.IsNullOrEmpty(item.ThumbnailFileName))
            {
                var old = Path.Combine(uploadsDir, item.ThumbnailFileName);
                if (System.IO.File.Exists(old))
                {
                    try { System.IO.File.Delete(old); } catch { }
                }
            }

            var stored = $"{Path.GetFileNameWithoutExtension(req.File.FileName)}_{Guid.NewGuid()}{ext}";
            var full = Path.Combine(uploadsDir, stored);
            await using (var fs = System.IO.File.Create(full))
            {
                await req.File.CopyToAsync(fs);
            }

            item.ThumbnailFileName = stored;
            _imageRepository.Update(item);

            var baseU = $"{Request.Scheme}://{Request.Host}";
            return Ok(new
            {
                id = item.Id,
                thumbnailFileName = stored,
                thumbnailUrl = $"{baseU}/uploads/{stored}"
            });
        }

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
                try { System.IO.File.Delete(full); } catch { }
            }

            if (!string.IsNullOrEmpty(item.ThumbnailFileName))
            {
                var thumb = Path.Combine(uploadsDir, item.ThumbnailFileName);
                if (System.IO.File.Exists(thumb))
                {
                    try { System.IO.File.Delete(thumb); } catch { }
                }
            }

            return NoContent();
        }

        private void ValidateFileUpload(ImageUploadRequestDTO request)
        {
            var allowed = VideoExts.Concat(ImageExts).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var ext = Path.GetExtension(request.File.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
                ModelState.AddModelError("file", "Unsupported file extension.");

            const long limit = 500L * 1024 * 1024; // 500MB
            if (request.File.Length > limit)
                ModelState.AddModelError("file", $"File size exceeds the limit of {limit / 1024 / 1024}MB.");
        }
    }
}
