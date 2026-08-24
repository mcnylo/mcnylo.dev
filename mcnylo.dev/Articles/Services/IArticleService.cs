using mcnylo.dev.Articles.ViewModels;
using mcnylo.dev.Data.Models;

namespace mcnylo.dev.Articles.Services
{
    public interface IArticleService
    {
        public Task<Article?> GetPublishedArticleBySlugAsync(string slug);
        public Task<ArticlePagedResultVM> GetPublishedArticleResultsAsync(ArticleFilterVM filter);
        public Task<List<ArticleCategory>> GetArticleCategoriesAsync();
        public Task<List<Tag>> GetArticleTagsAsync();
    }
}
