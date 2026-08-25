using mcnylo.dev.Admin.ViewModels.Articles;
using mcnylo.dev.Admin.ViewModels.Tags;
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
        public Task<AdminArticleListVM> GetAdminArticleResultsAsync(string? search, string? status, int pageNumber, int pageSize);
        public Task<List<Tag>> GetAllTagsAsync();
        public Task<bool> ArticleSlugExistsAsync(string slug, int? excludedArticleId = null);
        public Task<int> CreateArticleAsync(Article article, List<int> tagIds);
        public Task<Article?> GetAdminArticleByIdAsync(int id);
        public Task UpdateArticleAsync(Article article, List<int> tagIds);
        public Task<Article?> GetAdminArticleDeleteDetailsAsync(int id);
        public Task DeleteArticleAsync(int id);
        public Task<AdminArticleCategoryListVM> GetAdminArticleCategoryResultsAsync(int pageNumber, int pageSize);
        public Task<bool> ArticleCategorySlugExistsAsync(string slug, int? excludedCategoryId = null);
        public Task<int> CreateArticleCategoryAsync(ArticleCategory category);
        public Task<ArticleCategory?> GetArticleCategoryByIdAsync(int id);
        public Task UpdateArticleCategoryAsync(ArticleCategory category);
        public Task<ArticleCategory?> GetArticleCategoryDeleteDetailsAsync(int id);
        public Task DeleteArticleCategoryAsync(int id);
        public Task<AdminTagListVM> GetAdminTagResultsAsync(int pageNumber, int pageSize);
        public Task<bool> TagSlugExistsAsync(string slug, int? excludedTagId = null);
        public Task<int> CreateTagAsync(Tag tag);
        public Task<Tag?> GetTagByIdAsync(int id);
        public Task UpdateTagAsync(Tag tag);
        public Task<Tag?> GetTagDeleteDetailsAsync(int id);
        public Task DeleteTagAsync(int id);
        public Task<int> GetArticleTagUsageCountAsync(int tagId);
    }
}
