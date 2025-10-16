using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

        // POST: /api/Movie/Upload
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
                await request.File.CopyToAsync(fs);

            string? thumbName = null;
            if (VideoExts.Contains(ext))
            {
                try { thumbName = await _thumb.CreateFromVideoAsync(savePath, uploadsDir); }
                catch { /* best effort, ignore thumbnail failure */ }
            }

            var image = new Image
            {
                FileName = storedName,
                FileExtension = ext,
                FileSizeInBytes = request.File.Length,
                FileDescription = request.FileDescription,
                FilePath = storedName,
                ThumbnailFileName = thumbName,
                Title = null,
                Intro = null,
                Genre = null,
                Year = null,
                MovieId = null
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

        // PUT: /api/Movie/{id}/meta
        [HttpPut("{id:int}/meta")]
        public IActionResult UpdateMeta(int id, [FromBody] MediaMetaUpdateDTO dto)
        {
            var item = _imageRepository.GetById(id);
            if (item == null) return NotFound("Image not found");

            if (dto.Title != null) item.Title = dto.Title.Trim();
            if (dto.Intro != null) item.Intro = dto.Intro.Trim();
            if (dto.Genre != null) item.Genre = dto.Genre.Trim();
            if (dto.Year != null) item.Year = dto.Year;
            if (dto.MovieId != null) item.MovieId = dto.MovieId;

            _imageRepository.Update(item);

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            return Ok(new
            {
                id = item.Id,
                fileName = item.FileName,
                fileUrl = $"{baseUrl}/uploads/{item.FileName}",
                thumbnailUrl = !string.IsNullOrEmpty(item.ThumbnailFileName) ? $"{baseUrl}/uploads/{item.ThumbnailFileName}" : null,
                title = item.Title,
                intro = item.Intro,
                genre = item.Genre,
                year = item.Year,
                movieId = item.MovieId
            });
        }

        // GET: /api/Movie/GetMediaPaged
        [HttpGet("GetMediaPaged")]
        public IActionResult GetMediaPaged(
            [FromQuery] int? movieId,
            [FromQuery] string? type = "all",
            [FromQuery] string? q = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 24)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 24;

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var all = _imageRepository.GetAllInfoImages().AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
                all = all.Where(x =>
                    (x.FileDescription ?? "").Contains(q) ||
                    (x.Title ?? "").Contains(q) ||
                    (x.Intro ?? "").Contains(q) ||
                    (x.Genre ?? "").Contains(q)
                );

            if (movieId.HasValue && movieId > 0)
                all = all.Where(x => x.MovieId == movieId.Value);

            var kind = (type ?? "all").Trim().ToLowerInvariant();
            if (kind == "video")
                all = all.Where(x => VideoExts.Contains((x.FileExtension ?? "").ToLower()));
            else if (kind == "image")
                all = all.Where(x => ImageExts.Contains((x.FileExtension ?? "").ToLower()));

            var totalCount = all.Count();

            var items = all
                .OrderByDescending(x => x.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new {
                    id = x.Id,
                    fileName = x.FileName,
                    fileExtension = x.FileExtension,
                    fileSizeInBytes = x.FileSizeInBytes,
                    fileDescription = x.FileDescription,
                    fileUrl = $"{baseUrl}/uploads/{x.FileName}",
                    thumbnailUrl = x.ThumbnailFileName != null ? $"{baseUrl}/uploads/{x.ThumbnailFileName}" : null,
                    title = x.Title,
                    intro = x.Intro,
                    genre = x.Genre,
                    year = x.Year,
                    movieId = x.MovieId
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
                thumbnailUrl = !string.IsNullOrEmpty(x.ThumbnailFileName) ? $"{baseUrl}/uploads/{x.ThumbnailFileName}" : null,
                title = x.Title,
                intro = x.Intro,
                genre = x.Genre,
                year = x.Year,
                movieId = x.MovieId
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

            if (req.File != null && req.File.Length > 0)
            {
                ValidateFileUpload(new ImageUploadRequestDTO { File = req.File });
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var ext = Path.GetExtension(req.File.FileName).ToLowerInvariant();

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

                var newStoredName = $"{Guid.NewGuid()}{ext}";
                var newFullPath = Path.Combine(uploadsDir, newStoredName);
                await using (var fs = System.IO.File.Create(newFullPath))
                    await req.File.CopyToAsync(fs);

                string? newThumb = null;
                if (VideoExts.Contains(ext))
                {
                    try { newThumb = await _thumb.CreateFromVideoAsync(newFullPath, uploadsDir); }
                    catch { /* ignore */ }
                }

                item.FileName = newStoredName;
                item.FilePath = newStoredName;
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
                thumbnailUrl = !string.IsNullOrEmpty(item.ThumbnailFileName) ? $"{baseUrl}/uploads/{item.ThumbnailFileName}" : null,
                title = item.Title,
                intro = item.Intro,
                genre = item.Genre,
                year = item.Year,
                movieId = item.MovieId
            });
        }

        // POST: /api/Movie/{id}/thumbnail
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
            {
                await req.File.CopyToAsync(fs);
            }

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