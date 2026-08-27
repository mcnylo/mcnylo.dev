using System.Text.RegularExpressions;

namespace mcnylo.dev.Media.Services.Resume
{
    public class ResumePdfUploadService : IResumePdfUploadService
    {
        private const long MaxResumePdfSizeInBytes = 10 * 1024 * 1024;

        private readonly IMediaStorageService _mediaStorageService;

        // ========================================================================================

        public ResumePdfUploadService(IMediaStorageService mediaStorageService)
        {
            _mediaStorageService = mediaStorageService;
        }

        // ========================================================================================

        public async Task<ResumePdfUploadResult> SaveResumePdfAsync(IFormFile resumePdfFile)
        {
            if (resumePdfFile == null || resumePdfFile.Length == 0)
            {
                return ResumePdfUploadResult.Failure("Please select a resume PDF to upload.");
            }

            if (resumePdfFile.Length > MaxResumePdfSizeInBytes)
            {
                return ResumePdfUploadResult.Failure("Resume PDFs must be 10 MB or smaller.");
            }

            var extension = Path.GetExtension(resumePdfFile.FileName);

            if (!string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
            {
                return ResumePdfUploadResult.Failure("Only PDF files can be uploaded for the resume.");
            }

            Directory.CreateDirectory(_mediaStorageService.ResumeMediaRootPath);

            var fileName = BuildResumeFileName(resumePdfFile.FileName);
            var filePath = Path.Combine(_mediaStorageService.ResumeMediaRootPath, fileName);

            await using var fileStream = File.Create(filePath);
            await resumePdfFile.CopyToAsync(fileStream);

            DeleteOtherResumeFiles(filePath);

            return ResumePdfUploadResult.Success(_mediaStorageService.BuildResumeRequestPath(fileName));
        }

        // ========================================================================================

        private static string BuildResumeFileName(string originalFileName)
        {
            var baseFileName = Path.GetFileNameWithoutExtension(originalFileName).ToLowerInvariant();
            baseFileName = Regex.Replace(baseFileName, @"[^a-z0-9]+", "-").Trim('-');

            if (string.IsNullOrWhiteSpace(baseFileName))
            {
                baseFileName = "resume";
            }

            return $"{baseFileName}-{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
        }
        private void DeleteOtherResumeFiles(string currentResumeFilePath)
        {
            var currentFullPath = Path.GetFullPath(currentResumeFilePath);

            foreach (var filePath in Directory.EnumerateFiles(_mediaStorageService.ResumeMediaRootPath))
            {
                var fullFilePath = Path.GetFullPath(filePath);

                if (string.Equals(fullFilePath, currentFullPath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                File.Delete(fullFilePath);
            }
        }
    }
}
