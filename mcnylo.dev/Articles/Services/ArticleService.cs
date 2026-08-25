using mcnylo.dev.Admin.ViewModels;
using mcnylo.dev.Admin.ViewModels.Articles;
using mcnylo.dev.Admin.ViewModels.Tags;
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
        public async Task<AdminArticleListVM> GetAdminArticleResultsAsync(string? search, string? status, int pageNumber, int pageSize)
        {
            if (pageNumber < 1)
            {
                pageNumber = 1;
            }

            if (pageSize < 1)
            {
                pageSize = 10;
            }

            var normalizedSearch = search?.Trim() ?? "";
            var normalizedStatus = string.IsNullOrWhiteSpace(status) ? "all" : status.Trim().ToLowerInvariant();

            if (normalizedStatus is not "all" and not "published" and not "draft")
            {
                normalizedStatus = "all";
            }

            var query = _dbContext.Articles.AsNoTracking().Include(article => article.ArticleCategory).AsQueryable();

            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                query = query.Where(article => article.ArticleTitle.Contains(normalizedSearch) || article.ArticleSlug.Contains(normalizedSearch));
            }

            if (normalizedStatus == "published")
            {
                query = query.Where(article => article.IsPublished);
            }
            else if (normalizedStatus == "draft")
            {
                query = query.Where(article => !article.IsPublished);
            }

            var totalArticles = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalArticles / (double)pageSize);

            if (totalPages > 0 && pageNumber > totalPages)
            {
                pageNumber = totalPages;
            }

            var articles = await query
                .OrderByDescending(article => article.UpdatedOn ?? article.CreatedOn)
                .ThenByDescending(article => article.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(article => new AdminArticleListItemVM
                {
                    Id = article.Id,
                    ArticleTitle = article.ArticleTitle,
                    ArticleSlug = article.ArticleSlug,
                    CategoryName = article.ArticleCategory != null ? article.ArticleCategory.CategoryName : "",
                    IsPublished = article.IsPublished,
                    CreatedOn = article.CreatedOn,
                    UpdatedOn = article.UpdatedOn,
                    PublishedOn = article.PublishedOn
                })
                .ToListAsync();

            return new AdminArticleListVM
            {
                Search = normalizedSearch,
                Status = normalizedStatus,
                Articles = articles,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalArticles = totalArticles,
                TotalPages = totalPages
            };
        }
        public async Task<List<Tag>> GetAllTagsAsync()
        {
            return await _dbContext.Tags.AsNoTracking().OrderBy(tag => tag.TagName).ToListAsync();
        }
        public async Task<bool> ArticleSlugExistsAsync(string slug, int? excludedArticleId = null)
        {
            return await _dbContext.Articles.AsNoTracking()
                .AnyAsync(article => article.ArticleSlug == slug && (!excludedArticleId.HasValue || article.Id != excludedArticleId.Value));
        }
        public async Task<int> CreateArticleAsync(Article article, List<int> tagIds)
        {
            var selectedTagIds = tagIds.Distinct().ToList();

            var validTagIds = await _dbContext.Tags.AsNoTracking().Where(tag => selectedTagIds.Contains(tag.Id)).Select(tag => tag.Id).ToListAsync();

            article.ArticleTags = validTagIds
                .Select(tagId => new ArticleTag
                {
                    Article = article,
                    TagId = tagId
                })
                .ToList();

            _dbContext.Articles.Add(article);

            await _dbContext.SaveChangesAsync();

            return article.Id;
        }
        public async Task<Article?> GetAdminArticleByIdAsync(int id)
        {
            return await _dbContext.Articles
                .Include(article => article.ArticleCategory)
                .Include(article => article.ArticleTags)
                    .ThenInclude(articleTag => articleTag.Tag)
                .FirstOrDefaultAsync(article => article.Id == id);
        }
        public async Task UpdateArticleAsync(Article article, List<int> tagIds)
        {
            var selectedTagIds = tagIds.Distinct().ToList();

            var validTagIds = await _dbContext.Tags.AsNoTracking().Where(tag => selectedTagIds.Contains(tag.Id))
                .Select(tag => tag.Id)
                .ToListAsync();

            article.ArticleTags.Clear();

            foreach (var tagId in validTagIds)
            {
                article.ArticleTags.Add(new ArticleTag
                {
                    ArticleId = article.Id,
                    TagId = tagId
                });
            }

            await _dbContext.SaveChangesAsync();
        }
        public async Task<Article?> GetAdminArticleDeleteDetailsAsync(int id)
        {
            return await _dbContext.Articles.AsNoTracking().Include(article => article.ArticleCategory).FirstOrDefaultAsync(article => article.Id == id);
        }
        public async Task DeleteArticleAsync(int id)
        {
            var article = await _dbContext.Articles.FirstOrDefaultAsync(article => article.Id == id);

            if (article == null)
            {
                return;
            }

            _dbContext.Articles.Remove(article);

            await _dbContext.SaveChangesAsync();
        }
        public async Task<AdminArticleCategoryListVM> GetAdminArticleCategoryResultsAsync(int pageNumber, int pageSize)
        {
            if (pageNumber < 1)
            {
                pageNumber = 1;
            }

            if (pageSize < 1)
            {
                pageSize = 10;
            }

            var query = _dbContext.ArticleCategories.AsNoTracking();

            var totalCategories = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCategories / (double)pageSize);

            if (totalPages > 0 && pageNumber > totalPages)
            {
                pageNumber = totalPages;
            }

            var categories = await query
                .OrderBy(category => category.DisplayOrder)
                .ThenBy(category => category.CategoryName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(category => new AdminArticleCategoryListItemVM
                {
                    Id = category.Id,
                    CategoryName = category.CategoryName,
                    CategorySlug = category.CategorySlug,
                    DisplayOrder = category.DisplayOrder,
                    ArticleCount = category.Articles.Count
                })
                .ToListAsync();

            return new AdminArticleCategoryListVM
            {
                Categories = categories,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCategories = totalCategories,
                TotalPages = totalPages
            };
        }
        public async Task<bool> ArticleCategorySlugExistsAsync(string slug, int? excludedCategoryId = null)
        {
            return await _dbContext.ArticleCategories.AsNoTracking().AnyAsync(category => category.CategorySlug == slug && (!excludedCategoryId.HasValue || category.Id != excludedCategoryId.Value));
        }
        public async Task<int> CreateArticleCategoryAsync(ArticleCategory category)
        {
            _dbContext.ArticleCategories.Add(category);

            await _dbContext.SaveChangesAsync();

            return category.Id;
        }
        public async Task<ArticleCategory?> GetArticleCategoryByIdAsync(int id)
        {
            return await _dbContext.ArticleCategories.FirstOrDefaultAsync(category => category.Id == id);
        }
        public async Task UpdateArticleCategoryAsync(ArticleCategory category)
        {
            await _dbContext.SaveChangesAsync();
        }
        public async Task<ArticleCategory?> GetArticleCategoryDeleteDetailsAsync(int id)
        {
            return await _dbContext.ArticleCategories.AsNoTracking().Include(category => category.Articles).FirstOrDefaultAsync(category => category.Id == id);
        }
        public async Task DeleteArticleCategoryAsync(int id)
        {
            var category = await _dbContext.ArticleCategories.FirstOrDefaultAsync(category => category.Id == id);

            if (category == null)
            {
                return;
            }

            _dbContext.ArticleCategories.Remove(category);

            await _dbContext.SaveChangesAsync();
        }
        public async Task<AdminTagListVM> GetAdminTagResultsAsync(int pageNumber, int pageSize)
        {
            if (pageNumber < 1)
            {
                pageNumber = 1;
            }

            if (pageSize < 1)
            {
                pageSize = 10;
            }

            var query = _dbContext.Tags.AsNoTracking();

            var totalTags = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalTags / (double)pageSize);

            if (totalPages > 0 && pageNumber > totalPages)
            {
                pageNumber = totalPages;
            }

            var tags = await query
                .OrderBy(tag => tag.TagName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(tag => new AdminTagListItemVM
                {
                    Id = tag.Id,
                    TagName = tag.TagName,
                    TagSlug = tag.TagSlug,
                    ProjectCount = tag.ProjectTags.Count,
                    ArticleCount = _dbContext.ArticleTags.Count(articleTag => articleTag.TagId == tag.Id)
                })
                .ToListAsync();

            return new AdminTagListVM
            {
                Tags = tags,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalTags = totalTags,
                TotalPages = totalPages
            };
        }
        public async Task<bool> TagSlugExistsAsync(string slug, int? excludedTagId = null)
        {
            return await _dbContext.Tags.AsNoTracking().AnyAsync(tag => tag.TagSlug == slug && (!excludedTagId.HasValue || tag.Id != excludedTagId.Value));
        }
        public async Task<int> CreateTagAsync(Tag tag)
        {
            _dbContext.Tags.Add(tag);

            await _dbContext.SaveChangesAsync();

            return tag.Id;
        }
        public async Task<Tag?> GetTagByIdAsync(int id)
        {
            return await _dbContext.Tags.FirstOrDefaultAsync(tag => tag.Id == id);
        }
        public async Task UpdateTagAsync(Tag tag)
        {
            await _dbContext.SaveChangesAsync();
        }
        public async Task<Tag?> GetTagDeleteDetailsAsync(int id)
        {
            return await _dbContext.Tags.AsNoTracking().Include(tag => tag.ProjectTags).FirstOrDefaultAsync(tag => tag.Id == id);
        }
        public async Task DeleteTagAsync(int id)
        {
            var tag = await _dbContext.Tags.FirstOrDefaultAsync(tag => tag.Id == id);

            if (tag == null)
            {
                return;
            }

            _dbContext.Tags.Remove(tag);

            await _dbContext.SaveChangesAsync();
        }
        public async Task<int> GetArticleTagUsageCountAsync(int tagId)
        {
            return await _dbContext.ArticleTags.AsNoTracking().CountAsync(articleTag => articleTag.TagId == tagId);
        }
    }
}
