using mcnylo.dev.Articles.Services;
using mcnylo.dev.Articles.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace mcnylo.dev.Articles.Controllers
{
    public class ArticlesController : Controller
    {
        private readonly IArticleService _articleService;
        private readonly IArticleMarkdownService _markdownService;

        // ========================================================================================

        public ArticlesController(
            IArticleService articleService,
            IArticleMarkdownService markdownService)
        {
            _articleService = articleService;
            _markdownService = markdownService;
        }

        // ========================================================================================

        [HttpGet("/articles")]
        public async Task<IActionResult> Index(string? search = null, List<string>? categorySlugs = null, List<string>? tagSlugs = null, int pageNumber = 1, int pageSize = 6)
        {
            var filter = new ArticleFilterVM
            {
                Search = search,
                CategorySlugs = categorySlugs ?? [],
                TagSlugs = tagSlugs ?? [],
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var results = await _articleService.GetPublishedArticleResultsAsync(filter);
            var categories = await _articleService.GetArticleCategoriesAsync();
            var tags = await _articleService.GetArticleTagsAsync();

            var viewModel = new ArticleIndexVM
            {
                Search = filter.Search ?? "",
                SelectedCategorySlugs = filter.CategorySlugs.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList(),
                SelectedTagSlugs = filter.TagSlugs.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList(),

                Categories = categories
                    .Select(category => new ArticleCategoryFilterVM
                    {
                        CategoryName = category.CategoryName,
                        CategorySlug = category.CategorySlug
                    })
                    .ToList(),

                AvailableTags = tags
                    .Select(tag => new ArticleTagFilterVM
                    {
                        TagName = tag.TagName,
                        TagSlug = tag.TagSlug
                    })
                    .ToList(),

                Results = results
            };

            return View(viewModel);
        }

        [HttpGet("/articles/{slug}")]
        public async Task<IActionResult> Details(string slug)
        {
            var article = await _articleService.GetPublishedArticleBySlugAsync(slug);

            if (article == null)
            {
                return NotFound();
            }

            var vm = new ArticleDetailsVM();

            vm.ArticleTitle = article.ArticleTitle;
            vm.ArticleSlug = article.ArticleSlug;
            vm.ShortDescription = article.ShortDescription;
            vm.HtmlContent = _markdownService.RenderToHtml(article.MarkdownContent);
            vm.PublishedOn = article.PublishedOn;
            vm.CategoryName = article.ArticleCategory?.CategoryName ?? "";
            vm.CategorySlug = article.ArticleCategory?.CategorySlug ?? "";
            vm.PrimaryImagePath = string.IsNullOrWhiteSpace(article.PrimaryImagePath) ? "/images/thumb-placeholder.jpg" : article.PrimaryImagePath;
            vm.PrimaryImageAltText = string.IsNullOrWhiteSpace(article.PrimaryImageAltText) ? $"{article.ArticleTitle} thumbnail" : article.PrimaryImageAltText;
            vm.Tags = article.ArticleTags.Where(articleTag => articleTag.Tag != null)
                .OrderBy(articleTag => articleTag.Tag!.TagName)
                .Select(articleTag => new ArticleTagVM
                {
                    TagName = articleTag.Tag!.TagName,
                    TagSlug = articleTag.Tag.TagSlug
                })
                .ToList();

            return View(vm);
        }
    }
}
