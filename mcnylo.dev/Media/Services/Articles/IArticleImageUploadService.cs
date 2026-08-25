namespace mcnylo.dev.Media.Services.Articles
{
    public interface IArticleImageUploadService
    {
        Task<ArticleImageUploadResult> SaveArticleImageAsync(IFormFile imageFile, CancellationToken cancellationToken = default);
    }
}
