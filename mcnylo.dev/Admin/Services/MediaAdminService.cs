using mcnylo.dev.Admin.ViewModels.Media;
using mcnylo.dev.Data.Context;
using mcnylo.dev.Media.Services;
using Microsoft.EntityFrameworkCore;

namespace mcnylo.dev.Admin.Services
{
    public class MediaAdminService : IMediaAdminService
    {
        private readonly McNyloDbContext _dbContext;
        private readonly IMediaStorageService _mediaStorageService;

        // ========================================================================================

        public MediaAdminService(McNyloDbContext dbContext, IMediaStorageService mediaStorageService)
        {
            _dbContext = dbContext;
            _mediaStorageService = mediaStorageService;
        }

        // ========================================================================================

        public async Task<AdminMediaListVM> GetAdminMediaListAsync(int pageNumber, int pageSize)
        {
            var mediaItems = await BuildAdminMediaItemsAsync();

            var orderedMediaItems = mediaItems.OrderByDescending(media => media.LastModifiedOn).ThenBy(media => media.RequestPath).ToList();

            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var totalMediaItems = orderedMediaItems.Count;
            var totalPages = (int)Math.Ceiling(totalMediaItems / (double)pageSize);

            if (totalPages > 0 && pageNumber > totalPages)
            {
                pageNumber = totalPages;
            }

            return new AdminMediaListVM
            {
                MediaItems = orderedMediaItems
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalMediaItems = totalMediaItems,
                TotalPages = totalPages,
                TotalReferencedMediaItems = orderedMediaItems.Count(media => media.IsReferenced),
                TotalUnreferencedMediaItems = orderedMediaItems.Count(media => !media.IsReferenced)
            };
        }
        public async Task<AdminMediaDeleteVM?> GetAdminMediaDeleteDetailsAsync(string relativePath)
        {
            relativePath = NormalizeRelativePath(relativePath);

            var mediaItems = await BuildAdminMediaItemsAsync();

            var mediaItem = mediaItems.FirstOrDefault(mediaItem =>
                string.Equals(mediaItem.RelativePath, relativePath, StringComparison.OrdinalIgnoreCase));

            if (mediaItem == null)
            {
                return null;
            }

            return new AdminMediaDeleteVM
            {
                FileName = mediaItem.FileName,
                RequestPath = mediaItem.RequestPath,
                RelativePath = mediaItem.RelativePath,
                MediaArea = mediaItem.MediaArea,
                FileSizeInBytes = mediaItem.FileSizeInBytes,
                LastModifiedOn = mediaItem.LastModifiedOn,
                ArticleReferenceCount = mediaItem.ArticleReferenceCount,
                ProjectReferenceCount = mediaItem.ProjectReferenceCount
            };
        }
        public async Task<bool> DeleteMediaAsync(string relativePath)
        {
            var mediaItem = await GetAdminMediaDeleteDetailsAsync(relativePath);

            if (mediaItem == null || mediaItem.IsReferenced)
            {
                return false;
            }

            if (!TryBuildMediaFilePath(mediaItem.RelativePath, out var filePath))
            {
                return false;
            }

            if (!File.Exists(filePath))
            {
                return false;
            }

            File.Delete(filePath);

            return true;
        }

        // ========================================================================================

        private List<AdminMediaListItemVM> BuildMediaItems(
            string rootPath,
            string requestPathSegment,
            string mediaArea,
            List<ArticleMediaReferenceVM> articleReferences,
            List<ProjectMediaReferenceVM> projectReferences)
        {
            if (!Directory.Exists(rootPath))
            {
                return [];
            }

            return Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories)
                .Select(filePath => BuildMediaItem(filePath, rootPath, requestPathSegment, mediaArea, articleReferences, projectReferences))
                .ToList();
        }
        private AdminMediaListItemVM BuildMediaItem(
            string filePath,
            string rootPath,
            string requestPathSegment,
            string mediaArea,
            List<ArticleMediaReferenceVM> articleReferences,
            List<ProjectMediaReferenceVM> projectReferences)
        {
            var fileInfo = new FileInfo(filePath);
            var relativeFilePath = Path.GetRelativePath(rootPath, filePath).Replace('\\', '/');
            var relativePath = $"{requestPathSegment}/{relativeFilePath}";
            var requestPath = $"{_mediaStorageService.RequestPath}/{relativePath}";

            return new AdminMediaListItemVM
            {
                FileName = fileInfo.Name,
                RequestPath = requestPath,
                RelativePath = relativePath,
                MediaArea = mediaArea,
                FileSizeInBytes = fileInfo.Length,
                LastModifiedOn = fileInfo.LastWriteTimeUtc,
                ArticleReferenceCount = articleReferences.Count(article => string.Equals(article.PrimaryImagePath, requestPath, StringComparison.OrdinalIgnoreCase)
                    || article.MarkdownContent?.Contains(requestPath, StringComparison.OrdinalIgnoreCase) == true),
                ProjectReferenceCount = projectReferences
                    .Where(projectMedia => string.Equals(projectMedia.MediaURL, requestPath, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(projectMedia.ThumbnailURL, requestPath, StringComparison.OrdinalIgnoreCase))
                    .Select(projectMedia => projectMedia.ProjectId)
                    .Distinct()
                    .Count()
            };
        }
        private static string NormalizeRelativePath(string relativePath)
        {
            return (relativePath ?? "").Trim().TrimStart('/').Replace('\\', '/');
        }
        private async Task<List<AdminMediaListItemVM>> BuildAdminMediaItemsAsync()
        {
            var articleReferences = await _dbContext.Articles.AsNoTracking()
                .Select(article => new ArticleMediaReferenceVM
                {
                    PrimaryImagePath = article.PrimaryImagePath,
                    MarkdownContent = article.MarkdownContent
                })
                .ToListAsync();

            var projectReferences = await _dbContext.ProjectMedia.AsNoTracking()
                .Select(media => new ProjectMediaReferenceVM
                {
                    ProjectId = media.ProjectId,
                    MediaURL = media.MediaURL,
                    ThumbnailURL = media.ThumbnailURL
                })
                .ToListAsync();

            var mediaItems = new List<AdminMediaListItemVM>();

            mediaItems.AddRange(BuildMediaItems(
                _mediaStorageService.ArticleMediaRootPath,
                "articles",
                "Articles",
                articleReferences,
                projectReferences));

            mediaItems.AddRange(BuildMediaItems(
                _mediaStorageService.ProjectMediaRootPath,
                "projects",
                "Projects",
                articleReferences,
                projectReferences));

            return mediaItems;
        }
        private bool TryBuildMediaFilePath(string relativePath, out string filePath)
        {
            filePath = "";

            relativePath = NormalizeRelativePath(relativePath);

            string rootPath;
            string fileRelativePath;

            if (relativePath.StartsWith("articles/", StringComparison.OrdinalIgnoreCase))
            {
                rootPath = _mediaStorageService.ArticleMediaRootPath;
                fileRelativePath = relativePath["articles/".Length..];
            }
            else if (relativePath.StartsWith("projects/", StringComparison.OrdinalIgnoreCase))
            {
                rootPath = _mediaStorageService.ProjectMediaRootPath;
                fileRelativePath = relativePath["projects/".Length..];
            }
            else
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(fileRelativePath))
            {
                return false;
            }

            var fullRootPath = Path.GetFullPath(rootPath);
            var fullFilePath = Path.GetFullPath(Path.Combine(fullRootPath, fileRelativePath.Replace('/', Path.DirectorySeparatorChar)));

            if (!fullFilePath.StartsWith(fullRootPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            filePath = fullFilePath;

            return true;
        }
    }
}
