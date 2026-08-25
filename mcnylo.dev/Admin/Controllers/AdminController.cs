using mcnylo.dev.Admin.ViewModels;
using mcnylo.dev.Admin.ViewModels.Articles;
using mcnylo.dev.Admin.ViewModels.Tags;
using mcnylo.dev.Articles.Services;
using mcnylo.dev.Data.Models;
using mcnylo.dev.Media.Services.Articles;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace mcnylo.dev.Admin.Controllers
{
    public class AdminController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IArticleService _articleService;
        private readonly IArticleImageUploadService _articleImageUploadService;
        private readonly IArticleMarkdownService _markdownService;

        // ========================================================================================

        public AdminController(IConfiguration configuration, 
            IArticleService articleService, 
            IArticleImageUploadService articleImageUploadService,
            IArticleMarkdownService markdownService)
        {
            _configuration = configuration;
            _articleService = articleService;
            _articleImageUploadService = articleImageUploadService;
            _markdownService = markdownService;
        }

        // ========================================================================================

        [Authorize]
        [HttpGet("/admin")]
        public IActionResult Index()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpGet("/admin/login")]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index");
            }

            return View(new AdminLoginVM  { ReturnUrl = returnUrl });
        }

        [AllowAnonymous]
        [EnableRateLimiting("AdminLogin")]
        [ValidateAntiForgeryToken]
        [HttpPost("/admin/login")]
        public async Task<IActionResult> Login(AdminLoginVM vm)
        {
            var configUsername = _configuration["AdminAuth:Username"];
            var configPassword = _configuration["AdminAuth:Password"];

            if (string.IsNullOrWhiteSpace(configUsername) || string.IsNullOrWhiteSpace(configPassword))
            {
                vm.ErrorMessage = "Admin login is not configured.";
                vm.Password = "";

                return View(vm);
            }

            if (!SecureEquals(vm.Username, configUsername) || !SecureEquals(vm.Password, configPassword))
            {
                vm.ErrorMessage = "Invalid username or password.";
                vm.Password = "";

                return View(vm);
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, configUsername),
                new(ClaimTypes.Role, "Admin")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            if (!string.IsNullOrWhiteSpace(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
            {
                return Redirect(vm.ReturnUrl);
            }

            return RedirectToAction("Index");
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost("/admin/logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Login");
        }

        [Authorize]
        [HttpGet("/admin/articles")]
        public async Task<IActionResult> Articles(string? search = null, string? status = "all", int pageNumber = 1, int pageSize = 10)
        {
            var viewModel = await _articleService.GetAdminArticleResultsAsync(search, status, pageNumber, pageSize);

            return View(viewModel);
        }

        [Authorize]
        [HttpGet("/admin/articles/create")]
        public async Task<IActionResult> CreateArticle()
        {
            return View(await PopulateArticleFormOptionsAsync(new AdminArticleFormVM()));
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost("/admin/articles/create")]
        public async Task<IActionResult> CreateArticle(AdminArticleFormVM vm)
        {
            vm.ArticleTitle = vm.ArticleTitle?.Trim() ?? "";
            vm.ArticleSlug = vm.ArticleSlug?.Trim().ToLowerInvariant() ?? "";
            vm.ShortDescription = vm.ShortDescription?.Trim() ?? "";
            vm.MarkdownContent = vm.MarkdownContent?.Trim() ?? "";
            vm.PrimaryImagePath = vm.PrimaryImagePath?.Trim() ?? "";
            vm.PrimaryImageAltText = vm.PrimaryImageAltText?.Trim() ?? "";

            if (await _articleService.ArticleSlugExistsAsync(vm.ArticleSlug))
            {
                ModelState.AddModelError(nameof(vm.ArticleSlug), "An article with this slug already exists.");
            }

            var categoryIds = (await _articleService.GetArticleCategoriesAsync()).Select(category => category.Id).ToHashSet();

            if (vm.ArticleCategoryId.HasValue && !categoryIds.Contains(vm.ArticleCategoryId.Value))
            {
                ModelState.AddModelError(nameof(vm.ArticleCategoryId), "Select a valid category.");
            }

            if (!ModelState.IsValid)
            {
                return View(await PopulateArticleFormOptionsAsync(vm));
            }

            var now = DateTime.UtcNow;

            if (vm.PrimaryImageFile != null && vm.PrimaryImageFile.Length > 0)
            {
                var uploadResult = await _articleImageUploadService.SaveArticleImageAsync(vm.PrimaryImageFile);

                if (!uploadResult.Succeeded)
                {
                    ModelState.AddModelError(nameof(vm.PrimaryImageFile), uploadResult.ErrorMessage);

                    return View(await PopulateArticleFormOptionsAsync(vm));
                }

                vm.PrimaryImagePath = uploadResult.RequestPath;
            }

            var article = new Article
            {
                ArticleTitle = vm.ArticleTitle,
                ArticleSlug = vm.ArticleSlug,
                ShortDescription = vm.ShortDescription,
                MarkdownContent = vm.MarkdownContent,
                ArticleCategoryId = vm.ArticleCategoryId,
                PrimaryImagePath = vm.PrimaryImagePath,
                PrimaryImageAltText = vm.PrimaryImageAltText,
                IsPublished = vm.IsPublished,
                CreatedOn = now,
                PublishedOn = vm.IsPublished ? now : null
            };

            await _articleService.CreateArticleAsync(article, vm.SelectedTagIds);

            return RedirectToAction("Articles");
        }

        [Authorize]
        [HttpGet("/admin/articles/{id:int}/edit")]
        public async Task<IActionResult> EditArticle(int id)
        {
            var article = await _articleService.GetAdminArticleByIdAsync(id);

            if (article == null)
            {
                return NotFound();
            }

            var vm = new AdminArticleFormVM
            {
                Id = article.Id,
                ArticleTitle = article.ArticleTitle,
                ArticleSlug = article.ArticleSlug,
                ShortDescription = article.ShortDescription,
                MarkdownContent = article.MarkdownContent,
                ArticleCategoryId = article.ArticleCategoryId,
                PrimaryImagePath = article.PrimaryImagePath,
                PrimaryImageAltText = article.PrimaryImageAltText,
                IsPublished = article.IsPublished,
                SelectedTagIds = article.ArticleTags.Select(articleTag => articleTag.TagId).ToList()
            };

            return View(await PopulateArticleFormOptionsAsync(vm));
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost("/admin/articles/{id:int}/edit")]
        public async Task<IActionResult> EditArticle(int id, AdminArticleFormVM vm)
        {
            if (id != vm.Id)
            {
                return BadRequest();
            }

            var article = await _articleService.GetAdminArticleByIdAsync(id);

            if (article == null)
            {
                return NotFound();
            }

            vm.ArticleTitle = vm.ArticleTitle?.Trim() ?? "";
            vm.ArticleSlug = vm.ArticleSlug?.Trim().ToLowerInvariant() ?? "";
            vm.ShortDescription = vm.ShortDescription?.Trim() ?? "";
            vm.MarkdownContent = vm.MarkdownContent?.Trim() ?? "";
            vm.PrimaryImagePath = vm.PrimaryImagePath?.Trim() ?? "";
            vm.PrimaryImageAltText = vm.PrimaryImageAltText?.Trim() ?? "";

            if (await _articleService.ArticleSlugExistsAsync(vm.ArticleSlug, id))
            {
                ModelState.AddModelError(nameof(vm.ArticleSlug), "An article with this slug already exists.");
            }

            var categoryIds = (await _articleService.GetArticleCategoriesAsync()).Select(category => category.Id).ToHashSet();

            if (vm.ArticleCategoryId.HasValue && !categoryIds.Contains(vm.ArticleCategoryId.Value))
            {
                ModelState.AddModelError(nameof(vm.ArticleCategoryId), "Select a valid category.");
            }

            if (!ModelState.IsValid)
            {
                return View(await PopulateArticleFormOptionsAsync(vm));
            }

            if (vm.PrimaryImageFile != null && vm.PrimaryImageFile.Length > 0)
            {
                var uploadResult = await _articleImageUploadService.SaveArticleImageAsync(vm.PrimaryImageFile);

                if (!uploadResult.Succeeded)
                {
                    ModelState.AddModelError(nameof(vm.PrimaryImageFile), uploadResult.ErrorMessage);

                    return View(await PopulateArticleFormOptionsAsync(vm));
                }

                vm.PrimaryImagePath = uploadResult.RequestPath;
            }

            var wasPublished = article.IsPublished;
            var now = DateTime.UtcNow;

            article.ArticleTitle = vm.ArticleTitle;
            article.ArticleSlug = vm.ArticleSlug;
            article.ShortDescription = vm.ShortDescription;
            article.MarkdownContent = vm.MarkdownContent;
            article.ArticleCategoryId = vm.ArticleCategoryId;
            article.PrimaryImagePath = vm.PrimaryImagePath;
            article.PrimaryImageAltText = vm.PrimaryImageAltText;
            article.IsPublished = vm.IsPublished;
            article.UpdatedOn = now;

            if (!wasPublished && vm.IsPublished)
            {
                article.PublishedOn = now;
            }
            else if (wasPublished && !vm.IsPublished)
            {
                article.PublishedOn = null;
            }

            await _articleService.UpdateArticleAsync(article, vm.SelectedTagIds);

            return RedirectToAction("Articles");
        }

        [Authorize]
        [HttpGet("/admin/articles/{id:int}/delete")]
        public async Task<IActionResult> DeleteArticle(int id)
        {
            var article = await _articleService.GetAdminArticleDeleteDetailsAsync(id);

            if (article == null)
            {
                return NotFound();
            }

            var vm = new AdminArticleDeleteVM
            {
                Id = article.Id,
                ArticleTitle = article.ArticleTitle,
                ArticleSlug = article.ArticleSlug,
                CategoryName = article.ArticleCategory?.CategoryName ?? "",
                IsPublished = article.IsPublished,
                CreatedOn = article.CreatedOn,
                PublishedOn = article.PublishedOn
            };

            return View(vm);
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost("/admin/articles/{id:int}/delete")]
        public async Task<IActionResult> DeleteArticleConfirmed(int id)
        {
            await _articleService.DeleteArticleAsync(id);

            return RedirectToAction("Articles");
        }

        [Authorize]
        [HttpGet("/admin/articles/{id:int}/preview")]
        public async Task<IActionResult> PreviewArticle(int id)
        {
            var article = await _articleService.GetAdminArticleByIdAsync(id);

            if (article == null)
            {
                return NotFound();
            }

            var vm = new AdminArticlePreviewVM
            {
                Id = article.Id,
                ArticleTitle = article.ArticleTitle,
                ArticleSlug = article.ArticleSlug,
                ShortDescription = article.ShortDescription,
                HtmlContent = _markdownService.RenderToHtml(article.MarkdownContent),
                IsPublished = article.IsPublished,
                PublishedOn = article.PublishedOn,
                CategoryName = article.ArticleCategory?.CategoryName ?? "",
                PrimaryImagePath = string.IsNullOrWhiteSpace(article.PrimaryImagePath) ? "/images/thumb-placeholder.jpg" : article.PrimaryImagePath,
                PrimaryImageAltText = string.IsNullOrWhiteSpace(article.PrimaryImageAltText) ? $"{article.ArticleTitle} thumbnail" : article.PrimaryImageAltText,
                Tags = article.ArticleTags
                    .Where(articleTag => articleTag.Tag != null)
                    .OrderBy(articleTag => articleTag.Tag!.TagName)
                    .Select(articleTag => articleTag.Tag!.TagName)
                    .ToList()
            };

            return View(vm);
        }

        [Authorize]
        [HttpGet("/admin/article-categories")]
        public async Task<IActionResult> ArticleCategories(int pageNumber = 1, int pageSize = 10)
        {
            var viewModel = await _articleService.GetAdminArticleCategoryResultsAsync(pageNumber, pageSize);

            return View(viewModel);
        }

        [Authorize]
        [HttpGet("/admin/article-categories/create")]
        public IActionResult CreateArticleCategory()
        {
            return View(new AdminArticleCategoryFormVM());
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost("/admin/article-categories/create")]
        public async Task<IActionResult> CreateArticleCategory(AdminArticleCategoryFormVM vm)
        {
            vm.CategoryName = vm.CategoryName?.Trim() ?? "";
            vm.CategorySlug = vm.CategorySlug?.Trim().ToLowerInvariant() ?? "";

            if (await _articleService.ArticleCategorySlugExistsAsync(vm.CategorySlug))
            {
                ModelState.AddModelError(nameof(vm.CategorySlug), "An article category with this slug already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var category = new ArticleCategory
            {
                CategoryName = vm.CategoryName,
                CategorySlug = vm.CategorySlug,
                DisplayOrder = vm.DisplayOrder
            };

            await _articleService.CreateArticleCategoryAsync(category);

            return RedirectToAction(nameof(ArticleCategories));
        }

        [Authorize]
        [HttpGet("/admin/article-categories/{id:int}/edit")]
        public async Task<IActionResult> EditArticleCategory(int id)
        {
            var category = await _articleService.GetArticleCategoryByIdAsync(id);

            if (category == null)
            {
                return NotFound();
            }

            var vm = new AdminArticleCategoryFormVM
            {
                Id = category.Id,
                CategoryName = category.CategoryName,
                CategorySlug = category.CategorySlug,
                DisplayOrder = category.DisplayOrder
            };

            return View(vm);
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost("/admin/article-categories/{id:int}/edit")]
        public async Task<IActionResult> EditArticleCategory(int id, AdminArticleCategoryFormVM vm)
        {
            if (id != vm.Id)
            {
                return BadRequest();
            }

            vm.CategoryName = vm.CategoryName?.Trim() ?? "";
            vm.CategorySlug = vm.CategorySlug?.Trim().ToLowerInvariant() ?? "";

            var category = await _articleService.GetArticleCategoryByIdAsync(id);

            if (category == null)
            {
                return NotFound();
            }

            if (await _articleService.ArticleCategorySlugExistsAsync(vm.CategorySlug, id))
            {
                ModelState.AddModelError(nameof(vm.CategorySlug), "An article category with this slug already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            category.CategoryName = vm.CategoryName;
            category.CategorySlug = vm.CategorySlug;
            category.DisplayOrder = vm.DisplayOrder;

            await _articleService.UpdateArticleCategoryAsync(category);

            return RedirectToAction(nameof(ArticleCategories));
        }

        [Authorize]
        [HttpGet("/admin/article-categories/{id:int}/delete")]
        public async Task<IActionResult> DeleteArticleCategory(int id)
        {
            var category = await _articleService.GetArticleCategoryDeleteDetailsAsync(id);

            if (category == null)
            {
                return NotFound();
            }

            var vm = new AdminArticleCategoryDeleteVM
            {
                Id = category.Id,
                CategoryName = category.CategoryName,
                CategorySlug = category.CategorySlug,
                DisplayOrder = category.DisplayOrder,
                ArticleCount = category.Articles.Count
            };

            return View(vm);
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost("/admin/article-categories/{id:int}/delete")]
        public async Task<IActionResult> DeleteArticleCategoryConfirmed(int id)
        {
            await _articleService.DeleteArticleCategoryAsync(id);

            return RedirectToAction(nameof(ArticleCategories));
        }

        [Authorize]
        [HttpGet("/admin/tags")]
        public async Task<IActionResult> Tags(int pageNumber = 1, int pageSize = 10, string? returnUrl = null)
        {
            var vm = await _articleService.GetAdminTagResultsAsync(pageNumber, pageSize);

            vm.ReturnUrl = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/admin";

            return View(vm);
        }

        [Authorize]
        [HttpGet("/admin/tags/create")]
        public IActionResult CreateTag(string? returnUrl = null)
        {
            return View(new AdminTagFormVM
            {
                ReturnUrl = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                    ? returnUrl
                    : "/admin/tags"
            });
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost("/admin/tags/create")]
        public async Task<IActionResult> CreateTag(AdminTagFormVM vm)
        {
            vm.TagName = vm.TagName?.Trim() ?? "";
            vm.TagSlug = vm.TagSlug?.Trim().ToLowerInvariant() ?? "";

            vm.ReturnUrl = !string.IsNullOrWhiteSpace(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl) ? vm.ReturnUrl : "/admin/tags";

            if (await _articleService.TagSlugExistsAsync(vm.TagSlug))
            {
                ModelState.AddModelError(nameof(vm.TagSlug), "A tag with this slug already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var tag = new Tag
            {
                TagName = vm.TagName,
                TagSlug = vm.TagSlug
            };

            await _articleService.CreateTagAsync(tag);

            return Redirect(vm.ReturnUrl);
        }

        [Authorize]
        [HttpGet("/admin/tags/{id:int}/edit")]
        public async Task<IActionResult> EditTag(int id, string? returnUrl = null)
        {
            var tag = await _articleService.GetTagByIdAsync(id);

            if (tag == null)
            {
                return NotFound();
            }

            var vm = new AdminTagFormVM
            {
                Id = tag.Id,
                TagName = tag.TagName,
                TagSlug = tag.TagSlug,
                ReturnUrl = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/admin/tags"
            };

            return View(vm);
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost("/admin/tags/{id:int}/edit")]
        public async Task<IActionResult> EditTag(int id, AdminTagFormVM vm)
        {
            if (id != vm.Id)
            {
                return BadRequest();
            }

            vm.TagName = vm.TagName?.Trim() ?? "";
            vm.TagSlug = vm.TagSlug?.Trim().ToLowerInvariant() ?? "";
            vm.ReturnUrl = !string.IsNullOrWhiteSpace(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl) ? vm.ReturnUrl : "/admin/tags";

            var tag = await _articleService.GetTagByIdAsync(id);

            if (tag == null)
            {
                return NotFound();
            }

            if (await _articleService.TagSlugExistsAsync(vm.TagSlug, id))
            {
                ModelState.AddModelError(nameof(vm.TagSlug), "A tag with this slug already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            tag.TagName = vm.TagName;
            tag.TagSlug = vm.TagSlug;

            await _articleService.UpdateTagAsync(tag);

            return Redirect(vm.ReturnUrl);
        }

        [Authorize]
        [HttpGet("/admin/tags/{id:int}/delete")]
        public async Task<IActionResult> DeleteTag(int id, string? returnUrl = null)
        {
            var tag = await _articleService.GetTagDeleteDetailsAsync(id);

            if (tag == null)
            {
                return NotFound();
            }

            var vm = new AdminTagDeleteVM
            {
                Id = tag.Id,
                TagName = tag.TagName,
                TagSlug = tag.TagSlug,
                ProjectCount = tag.ProjectTags.Count,
                ArticleCount = await _articleService.GetArticleTagUsageCountAsync(tag.Id),
                ReturnUrl = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/admin/tags"
            };

            return View(vm);
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost("/admin/tags/{id:int}/delete")]
        public async Task<IActionResult> DeleteTagConfirmed(int id, AdminTagDeleteVM vm)
        {
            vm.ReturnUrl = !string.IsNullOrWhiteSpace(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl) ? vm.ReturnUrl : "/admin/tags";

            await _articleService.DeleteTagAsync(id);

            return Redirect(vm.ReturnUrl);
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost("/admin/articles/inline-image")]
        public async Task<IActionResult> UploadArticleInlineImage(IFormFile? imageFile, string? altText)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                return BadRequest(new { errorMessage = "Choose an image to upload." });
            }

            var uploadResult = await _articleImageUploadService.SaveArticleImageAsync(imageFile);

            if (!uploadResult.Succeeded)
            {
                return BadRequest(new { errorMessage = uploadResult.ErrorMessage });
            }

            var safeAltText = (altText ?? "")
                .Trim()
                .Replace("[", "")
                .Replace("]", "")
                .Replace("\r", " ")
                .Replace("\n", " ");

            var markdownSnippet = $"![{safeAltText}]({uploadResult.RequestPath})";

            return Json(new
            {
                markdownSnippet,
                imagePath = uploadResult.RequestPath
            });
        }

        // ========================================================================================

        private static bool SecureEquals(string? value, string? expectedValue)
        {
            var valueBytes = Encoding.UTF8.GetBytes(value ?? "");
            var expectedBytes = Encoding.UTF8.GetBytes(expectedValue ?? "");

            return CryptographicOperations.FixedTimeEquals(SHA256.HashData(valueBytes), SHA256.HashData(expectedBytes));
        }
        private async Task<AdminArticleFormVM> PopulateArticleFormOptionsAsync(AdminArticleFormVM vm)
        {
            vm.Categories = (await _articleService.GetArticleCategoriesAsync())
                .Select(category => new AdminArticleCategoryOptionVM
                {
                    Id = category.Id,
                    CategoryName = category.CategoryName
                })
                .ToList();

            vm.AvailableTags = (await _articleService.GetAllTagsAsync())
                .Select(tag => new AdminArticleTagOptionVM
                {
                    Id = tag.Id,
                    TagName = tag.TagName
                })
                .ToList();

            return vm;
        }
    }
}
