using mcnylo.dev.Articles.ViewModels;
using mcnylo.dev.Data.Context;
using mcnylo.dev.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace mcnylo.dev.Articles.Services
{
    public class ArticleService : IArticleService
    {
        private readonly McNyloDbContext _dbContext;

        // ========================================================================================

        public ArticleService(McNyloDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // ========================================================================================

        public async Task<Article?> GetPublishedArticleBySlugAsync(string slug)
        {
            return await _dbContext.Articles
                .AsNoTracking()
                .Include(article => article.ArticleCategory)
                .Include(article => article.ArticleTags)
                    .ThenInclude(articleTag => articleTag.Tag)
                .FirstOrDefaultAsync(article =>
                    article.ArticleSlug == slug &&
                    article.IsPublished);
        }
        public async Task<ArticlePagedResultVM> GetPublishedArticleResultsAsync(ArticleFilterVM filter)
        {
            if (filter.PageNumber < 1)
            {
                filter.PageNumber = 1;
            }

            if (filter.PageSize < 1)
            {
                filter.PageSize = 6;
            }

            var query = _dbContext.Articles.AsNoTracking()
                .Include(article => article.ArticleCategory)
                .Include(article => article.ArticleTags)
                    .ThenInclude(articleTag => articleTag.Tag)
                .Where(article => article.IsPublished);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = filter.Search.Trim();

                query = query.Where(x => x.ArticleTitle.Contains(search));
            }

            var selectedCategorySlugs = filter.CategorySlugs.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();

            if (selectedCategorySlugs.Count > 0)
            {
                query = query.Where(x => x.ArticleCategory != null && selectedCategorySlugs.Contains(x.ArticleCategory.CategorySlug));
            }

            var selectedTagSlugs = filter.TagSlugs.Where(tagSlug => !string.IsNullOrWhiteSpace(tagSlug)).Distinct().ToList();

            if (selectedTagSlugs.Count > 0)
            {
                query = query.Where(x => x.ArticleTags.Any(tag => tag.Tag != null && selectedTagSlugs.Contains(tag.Tag.TagSlug)));
            }

            var totalArticles = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalArticles / (double)filter.PageSize);

            if (totalPages > 0 && filter.PageNumber > totalPages)
            {
                filter.PageNumber = totalPages;
            }

            var articles = await query
                .OrderByDescending(article => article.PublishedOn)
                .ThenByDescending(article => article.CreatedOn)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(article => new ArticleListItemVM
                {
                    ArticleTitle = article.ArticleTitle,
                    ArticleSlug = article.ArticleSlug,
                    ShortDescription = article.ShortDescription,
                    PublishedOn = article.PublishedOn,
                    CategoryName = article.ArticleCategory != null ? article.ArticleCategory.CategoryName : "",
                    CategorySlug = article.ArticleCategory != null ? article.ArticleCategory.CategorySlug : "",
                    PrimaryImagePath = string.IsNullOrWhiteSpace(article.PrimaryImagePath) ? "/images/thumb-placeholder.jpg" : article.PrimaryImagePath,
                    PrimaryImageAltText = string.IsNullOrWhiteSpace(article.PrimaryImageAltText) ? $"{article.ArticleTitle} thumbnail" : article.PrimaryImageAltText,
                    Tags = article.ArticleTags
                        .Where(articleTag => articleTag.Tag != null)
                        .OrderBy(articleTag => articleTag.Tag!.TagName)
                        .Select(articleTag => new ArticleTagVM
                        {
                            TagName = articleTag.Tag!.TagName,
                            TagSlug = articleTag.Tag.TagSlug
                        })
                        .ToList()
                })
                .ToListAsync();

            return new ArticlePagedResultVM
            {
                Articles = articles,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalArticles = totalArticles,
                TotalPages = totalPages
            };
        }
        public async Task<List<ArticleCategory>> GetArticleCategoriesAsync()
        {
            return await _dbContext.ArticleCategories.AsNoTracking().OrderBy(x => x.DisplayOrder).ThenBy(x => x.CategoryName).ToListAsync();
        }
        public async Task<List<Tag>> GetArticleTagsAsync()
        {
            return await _dbContext.ArticleTags.AsNoTracking()
                .Include(articleTag => articleTag.Tag)
                .Where(articleTag => articleTag.Tag != null)
                .Select(articleTag => articleTag.Tag!)
                .Distinct()
                .OrderBy(tag => tag.TagName)
                .ToListAsync();
        }
    }
}
