namespace mcnylo.dev.Media.Services
{
    public interface IMediaStorageService
    {
        string RootPath { get; }
        string RequestPath { get; }
        string ArticleMediaRootPath { get; }
        string ProjectMediaRootPath { get; }
        string ResumeMediaRootPath { get; }

        string BuildArticleRequestPath(string fileName);
        string BuildProjectRequestPath(string fileName);
        string BuildResumeRequestPath(string fileName);
    }
}
