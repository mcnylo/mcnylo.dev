namespace mcnylo.dev.Media.Services
{
    public class MediaStorageService : IMediaStorageService
    {
        public string RootPath { get; }
        public string RequestPath { get; }
        public string ArticleMediaRootPath { get; }
        public string ProjectMediaRootPath { get; }
        public string ResumeMediaRootPath { get; }

        public MediaStorageService(IConfiguration configuration)
        {
            var rootPath = configuration["MediaStorage:RootPath"] ?? throw new InvalidOperationException("Media root path is not configured.");
            var requestPath = configuration["MediaStorage:RequestPath"] ?? throw new InvalidOperationException("Media request path is not configured.");

            rootPath = Path.GetFullPath(rootPath);

            if (!Path.IsPathFullyQualified(rootPath))
            {
                throw new InvalidOperationException("Media root path must be an absolute path.");
            }

            if (!requestPath.StartsWith('/'))
            {
                throw new InvalidOperationException("Media request path must start with '/'.");
            }

            RootPath = rootPath;
            RequestPath = requestPath.TrimEnd('/');
            ArticleMediaRootPath = Path.Combine(RootPath, "articles");
            ProjectMediaRootPath = Path.Combine(RootPath, "projects");
            ResumeMediaRootPath = Path.Combine(RootPath, "resume");

            Directory.CreateDirectory(RootPath);
            Directory.CreateDirectory(ArticleMediaRootPath);
            Directory.CreateDirectory(ProjectMediaRootPath);
            Directory.CreateDirectory(ResumeMediaRootPath);
        }

        public string BuildArticleRequestPath(string fileName)
        {
            return $"{RequestPath}/articles/{fileName}";
        }

        public string BuildProjectRequestPath(string fileName)
        {
            return $"{RequestPath}/projects/{fileName}";
        }

        public string BuildResumeRequestPath(string fileName)
        {
            return $"{RequestPath}/resume/{fileName}";
        }
    }
}
