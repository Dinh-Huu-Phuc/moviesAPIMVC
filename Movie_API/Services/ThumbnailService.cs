using Xabe.FFmpeg;

namespace Movie_API.Services
{
    public interface IThumbnailService
    {
        Task<string?> CreateFromVideoAsync(string videoFullPath, string outputDir);
    }

    public class ThumbnailService : IThumbnailService
    {
        public async Task<string?> CreateFromVideoAsync(string videoFullPath, string outputDir)
        {
            Directory.CreateDirectory(outputDir);
            var jpgName = $"{Path.GetFileNameWithoutExtension(videoFullPath)}.jpg";
            var outPath = Path.Combine(outputDir, jpgName);

            // Lấy frame ở giây thứ 1 (có thể chỉnh)
            var mediaInfo = await FFmpeg.GetMediaInfo(videoFullPath);
            var videoStream = mediaInfo.VideoStreams.FirstOrDefault();
            if (videoStream == null) return null;

            var snapshot = await FFmpeg.Conversions.New()
                .AddStream(videoStream)
                .SetSeek(TimeSpan.FromSeconds(1))
                .SetOutput(outPath)
                .Start();

            return jpgName; // chỉ trả tên file (không kèm path)
        }
    }
}
