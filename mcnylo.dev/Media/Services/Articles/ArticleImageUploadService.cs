using System.Security.Cryptography;

namespace mcnylo.dev.Media.Services.Articles
{
    public class ArticleImageUploadService : IArticleImageUploadService
    {
        private const long DefaultMaxImageBytes = 5 * 1024 * 1024;
        private readonly IMediaStorageService _mediaStorageService;
        private readonly long _maxImageBytes;
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };
        private static readonly Dictionary<string, string[]> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = ["image/jpeg"],
            [".jpeg"] = ["image/jpeg"],
            [".png"] = ["image/png"],
            [".webp"] = ["image/webp"]
        };

        // ========================================================================================

        public ArticleImageUploadService(
            IMediaStorageService mediaStorageService,
            IConfiguration configuration)
        {
            _mediaStorageService = mediaStorageService;
            _maxImageBytes = configuration.GetValue<long?>("MediaStorage:MaxArticleImageUploadBytes") ?? DefaultMaxImageBytes;
        }

        // ========================================================================================

        public async Task<ArticleImageUploadResult> SaveArticleImageAsync(IFormFile imageFile, CancellationToken cancellationToken = default)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                return ArticleImageUploadResult.Failure("Choose an image to upload.");
            }

            if (imageFile.Length > _maxImageBytes)
            {
                return ArticleImageUploadResult.Failure("Image must be 5 MB or smaller.");
            }

            await using var uploadStream = imageFile.OpenReadStream();

            var detectedExtension = await DetectImageExtensionAsync(uploadStream, cancellationToken);

            if (string.IsNullOrWhiteSpace(detectedExtension))
            {
                return ArticleImageUploadResult.Failure("Only valid JPG, PNG, and WEBP images are allowed.");
            }

            uploadStream.Position = 0;

            var extension = detectedExtension;

            var now = DateTime.UtcNow;
            var relativeFolder = Path.Combine(now.Year.ToString(), now.Month.ToString("00"));
            var targetDirectory = Path.Combine(_mediaStorageService.ArticleMediaRootPath, relativeFolder);

            Directory.CreateDirectory(targetDirectory);

            var storedFileName = $"{now:yyyyMMddHHmmss}-{CreateRandomToken()}{extension}";
            var targetPath = Path.Combine(targetDirectory, storedFileName);
            var fullTargetPath = Path.GetFullPath(targetPath);

            if (!fullTargetPath.StartsWith(_mediaStorageService.ArticleMediaRootPath, StringComparison.OrdinalIgnoreCase))
            {
                return ArticleImageUploadResult.Failure("Invalid image storage path.");
            }

            await using var targetStream = new FileStream(fullTargetPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);

            await uploadStream.CopyToAsync(targetStream, cancellationToken);

            var requestPath = $"{_mediaStorageService.RequestPath}/articles/{now.Year}/{now.Month:00}/{storedFileName}";

            return ArticleImageUploadResult.Success(requestPath, storedFileName);
        }

        // ========================================================================================

        private static async Task<string?> DetectImageExtensionAsync(Stream stream, CancellationToken cancellationToken)
        {
            var header = new byte[12];
            var bytesRead = await stream.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);

            if (bytesRead >= 3 &&
                header[0] == 0xFF &&
                header[1] == 0xD8 &&
                header[2] == 0xFF)
            {
                return ".jpg";
            }

            if (bytesRead >= 8 &&
                header[0] == 0x89 &&
                header[1] == 0x50 &&
                header[2] == 0x4E &&
                header[3] == 0x47 &&
                header[4] == 0x0D &&
                header[5] == 0x0A &&
                header[6] == 0x1A &&
                header[7] == 0x0A)
            {
                return ".png";
            }

            if (bytesRead >= 12 &&
                header[0] == 0x52 &&
                header[1] == 0x49 &&
                header[2] == 0x46 &&
                header[3] == 0x46 &&
                header[8] == 0x57 &&
                header[9] == 0x45 &&
                header[10] == 0x42 &&
                header[11] == 0x50)
            {
                return ".webp";
            }

            return null;
        }

        private static string CreateRandomToken()
        {
            return Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        }
    }
}
