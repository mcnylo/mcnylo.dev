using mcnylo.dev.About.Services;
using mcnylo.dev.Admin.Services;
using mcnylo.dev.Admin.ViewModels;
using mcnylo.dev.Admin.ViewModels.About;
using mcnylo.dev.Admin.ViewModels.Articles;
using mcnylo.dev.Admin.ViewModels.MFA;
using mcnylo.dev.Admin.ViewModels.Projects;
using mcnylo.dev.Admin.ViewModels.Tags;
using mcnylo.dev.Articles.Services;
using mcnylo.dev.Data.Models;
using mcnylo.dev.Media.Services.Articles;
using mcnylo.dev.Media.Services.Projects;
using mcnylo.dev.Media.Services.Resume;
using mcnylo.dev.Projects.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace mcnylo.dev.Admin.Controllers
{
    public class AdminController : Controller
    {
        private const string PendingMfaCookieName = "McNylo.PendingMfa";

        private readonly IConfiguration _configuration;
        private readonly IArticleService _articleService;
        private readonly IArticleImageUploadService _articleImageUploadService;
        private readonly IArticleMarkdownService _markdownService;
        private readonly IProjectService _projectService;
        private readonly IProjectImageUploadService _projectImageUploadService;
        private readonly IAboutService _aboutService;
        private readonly IMediaAdminService _mediaAdminService;
        private readonly IResumePdfUploadService _resumePdfUploadService;
        private readonly IAdminMfaService _adminMfaService;
        private readonly IDataProtector _pendingMfaProtector;

        // ========================================================================================

        public AdminController(IConfiguration configuration, 
            IArticleService articleService, 
            IArticleImageUploadService articleImageUploadService,
            IArticleMarkdownService markdownService,
            IProjectService projectService,
            IProjectImageUploadService projectImageUploadService,
            IAboutService aboutService,
            IMediaAdminService mediaAdminService,
            IResumePdfUploadService resumePdfUploadService,
            IAdminMfaService adminMfaService,
            IDataProtectionProvider dataProtectionProvider)
        {
            _configuration = configuration;
            _articleService = articleService;
            _articleImageUploadService = articleImageUploadService;
            _markdownService = markdownService;
            _projectService = projectService;
            _projectImageUploadService = projectImageUploadService;
            _aboutService = aboutService;
            _mediaAdminService = mediaAdminService;
            _resumePdfUploadService = resumePdfUploadService;
            _adminMfaService = adminMfaService;
            _pendingMfaProtector = dataProtectionProvider.CreateProtector("mcnylo.dev.Admin.PendingMfa.v1");
        }

        // ========================================================================================
        // LOGIN/MFA ==============================================================================

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
            ClearPendingMfaCookie();

            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction(nameof(Index));
            }

            return View(new AdminLoginVM { ReturnUrl = returnUrl });
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

            if (await _adminMfaService.IsMfaEnabledAsync(configUsername))
            {
                SetPendingMfaCookie(configUsername, vm.ReturnUrl);

                return Redirect("/admin/mfa");
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
            ClearPendingMfaCookie();

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction(nameof(Login));
        }

        [Authorize]
        [HttpGet("/admin/mfa/setup")]
        public async Task<IActionResult> MfaSetup()
        {
            var username = User.Identity?.Name;

            if (string.IsNullOrWhiteSpace(username))
            {
                return RedirectToAction(nameof(Login));
            }

            var setupInfo = await _adminMfaService.GetOrCreateSetupAsync(username);

            return View(new AdminMfaSetupVM
            {
                IsEnabled = setupInfo.IsEnabled,
                ManualEntryKey = setupInfo.ManualEntryKey,
                AuthenticatorUri = setupInfo.AuthenticatorUri,
                QrCodeImageDataUrl = setupInfo.QrCodeImageDataUrl
            });
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost("/admin/mfa/setup")]
        public async Task<IActionResult> MfaSetup(AdminMfaSetupVM vm)
        {
            var username = User.Identity?.Name;

            if (string.IsNullOrWhiteSpace(username))
            {
                return RedirectToAction("Index");
            }

            var result = await _adminMfaService.ConfirmSetupAsync(username, vm.VerificationCode);

            if (!result.Succeeded)
            {
                var setupInfo = await _adminMfaService.GetOrCreateSetupAsync(username);

                vm.IsEnabled = setupInfo.IsEnabled;
                vm.ManualEntryKey = setupInfo.ManualEntryKey;
                vm.AuthenticatorUri = setupInfo.AuthenticatorUri;
                vm.QrCodeImageDataUrl = setupInfo.QrCodeImageDataUrl;
                vm.VerificationCode = "";
                vm.ErrorMessage = result.ErrorMessage;

                return View(vm);
            }

            return View("MfaRecoveryCodes", new AdminMfaRecoveryCodesVM
            {
                RecoveryCodes = result.RecoveryCodes
            });
        }

        [AllowAnonymous]
        [HttpGet("/admin/mfa")]
        public IActionResult Mfa()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction(nameof(Index));
            }

            if (!TryGetPendingMfa(out _, out _))
            {
                return RedirectToAction(nameof(Login));
            }

            return View(new AdminMfaVerificationVM());
        }

        [AllowAnonymous]
        [EnableRateLimiting("AdminLogin")]
        [ValidateAntiForgeryToken]
        [HttpPost("/admin/mfa")]
        public async Task<IActionResult> Mfa(AdminMfaVerificationVM vm)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index");
            }

            if (!TryGetPendingMfa(out var username, out var returnUrl))
            {
                return RedirectToAction("Login");
            }

            var result = await _adminMfaService.VerifyLoginCodeAsync(username, vm.Code);

            if (!result.Succeeded)
            {
                vm.Code = "";
                vm.ErrorMessage = result.ErrorMessage;

                return View(vm);
            }

            ClearPendingMfaCookie();

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, username),
                new(ClaimTypes.Role, "Admin")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index");
        }

        // ARTICLES ===============================================================================

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

            return RedirectToAction("ArticleCategories");
        }

        // PROJECTS ===============================================================================

        [Authorize]
        [HttpGet("/admin/projects")]
        public async Task<IActionResult> Projects(string? search = null, int pageNumber = 1, int pageSize = 10)
        {
            var viewModel = await _projectService.GetAdminProjectResultsAsync(search, pageNumber, pageSize);

            return View(viewModel);
        }

        [Authorize]
        [HttpGet("/admin/project-categories")]
        public async Task<IActionResult> ProjectCategories(int pageNumber = 1, int pageSize = 10, string? returnUrl = null)
        {
            var vm = await _projectService.GetAdminProjectCategoryResultsAsync(pageNumber, pageSize);

            vm.ReturnUrl = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/admin/projects";

            return View(vm);
        }

        [Authorize]
        [HttpGet("/admin/project-categories/create")]
        public IActionResult CreateProjectCategory(string? returnUrl = null)
        {
            return View(new AdminProjectCategoryFormVM
            {
                ReturnUrl = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/admin/project-categories"
            });
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost("/admin/project-categories/create")]
        public async Task<IActionResult> CreateProjectCategory(AdminProjectCategoryFormVM vm)
        {
            vm.CategoryName = vm.CategoryName?.Trim() ?? "";
            vm.CategorySlug = vm.CategorySlug?.Trim().ToLowerInvariant() ?? "";
            vm.ReturnUrl = !string.IsNullOrWhiteSpace(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl) ? vm.ReturnUrl : "/admin/project-categories";

            if (await _projectService.ProjectCategorySlugExistsAsync(vm.CategorySlug))
            {
                ModelState.AddModelError(nameof(vm.CategorySlug), "A project category with this slug already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var category = new ProjectCategory
            {
                CategoryName = vm.CategoryName,
                CategorySlug = vm.CategorySlug
            };

            await _projectService.CreateProjectCategoryAsync(category);

            return Redirect(vm.ReturnUrl);
        }

        [Authorize]
        [HttpGet("/admin/project-categories/{id:int}/edit")]
        public async Task<IActionResult> EditProjectCategory(int id, string? returnUrl = null)
        {
            var category = await _projectService.GetProjectCategoryByIdAsync(id);

            if (category == null)
            {
                return NotFound();
            }

            var vm = new AdminProjectCategoryFormVM
            {
                Id = category.Id,
                CategoryName = category.CategoryName,
                CategorySlug = category.CategorySlug,
                ReturnUrl = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/admin/project-categories"
            };

            return View(vm);
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost("/admin/project-categories/{id:int}/edit")]
        public async Task<IActionResult> EditProjectCategory(int id, AdminProjectCategoryFormVM vm)
        {
            if (id != vm.Id)
            {
                return BadRequest();
            }

            vm.CategoryName = vm.CategoryName?.Trim() ?? "";
            vm.CategorySlug = vm.CategorySlug?.Trim().ToLowerInvariant() ?? "";
            vm.ReturnUrl = !string.IsNullOrWhiteSpace(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl) ? vm.ReturnUrl : "/admin/project-categories";

            var category = await _projectService.GetProjectCategoryByIdAsync(id);

            if (category == null)
            {
                return NotFound();
            }

            if (await _projectService.ProjectCategorySlugExistsAsync(vm.CategorySlug, id))
            {
                ModelState.AddModelError(nameof(vm.CategorySlug), "A project category with this slug already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            category.CategoryName = vm.CategoryName;
            category.CategorySlug = vm.CategorySlug;

            await _projectService.UpdateProjectCategoryAsync(category);

            return Redirect(vm.ReturnUrl);
        }

        [Authorize]
        [HttpGet("/admin/project-categories/{id:int}/delete")]
        public async Task<IActionResult> DeleteProjectCategory(int id, string? returnUrl = null)
        {
            var category = await _projectService.GetProjectCategoryDeleteDetailsAsync(id);

            if (category == null)
            {
                return NotFound();
            }

            var vm = new AdminProjectCategoryDeleteVM
            {
                Id = category.Id,
                CategoryName = category.CategoryName,
                CategorySlug = category.CategorySlug,
                ProjectCount = category.Projects.Count,
                ReturnUrl = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/admin/project-categories"
            };

            return View(vm);
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost("/admin/project-categories/{id:int}/delete")]
        public async Task<IActionResult> DeleteProjectCategoryConfirmed(int id, AdminProjectCategoryDeleteVM vm)
        {
            var category = await _projectService.GetProjectCategoryDeleteDetailsAsync(id);

            if (category == null)
            {
                return NotFound();
            }

            if (category.Projects.Count > 0)
            {
                ModelState.AddModelError("", "This category cannot be deleted while projects are using it.");

                vm.Id = category.Id;
                vm.CategoryName = category.CategoryName;
                vm.CategorySlug = category.CategorySlug;
                vm.ProjectCount = category.Projects.Count;

                return View("DeleteProjectCategory", vm);
            }

            await _projectService.DeleteProjectCategoryAsync(id);

            return RedirectToAction("ProjectCategories");
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

        [Authorize]
        [HttpGet("/admin/projects/create")]
        public async Task<IActionResult> CreateProject()
        {
            var vm = new AdminProjectFormVM
            {
                PrimaryMediaIndex = 0,
                MediaItems =
                [
                    new AdminProjectMediaFormVM
                    {
                        MediaType = "IMAGE",
                        SortOrder = 0,
                        PrimaryMediaIndex  = 0
                    }
                ]
            };

            return View(await PopulateProjectFormOptionsAsync(vm));
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost("/admin/projects/create")]
        public async Task<IActionResult> CreateProject(AdminProjectFormVM vm)
        {
            vm.ProjectTitle = vm.ProjectTitle?.Trim() ?? "";
            vm.ProjectSlug = vm.ProjectSlug?.Trim().ToLowerInvariant() ?? "";
            vm.ShortDescription = vm.ShortDescription?.Trim() ?? "";
            vm.LongDescription = vm.LongDescription?.Trim() ?? "";
            vm.RepositoryURL = vm.RepositoryURL?.Trim() ?? "";

            if (await _projectService.ProjectSlugExistsAsync(vm.ProjectSlug))
            {
                ModelState.AddModelError(nameof(vm.ProjectSlug), "A project with this slug already exists.");
            }

            var categoryIds = (await _projectService.GetProjectCategoriesAsync()).Select(category => category.Id).ToHashSet();

            if (!categoryIds.Contains(vm.CategoryId))
            {
                ModelState.AddModelError(nameof(vm.CategoryId), "Select a valid category.");
            }

            if (!string.IsNullOrWhiteSpace(vm.RepositoryURL)
                && (!Uri.TryCreate(vm.RepositoryURL, UriKind.Absolute, out var repositoryUri) || repositoryUri.Scheme is not ("http" or "https")))
            {
                ModelState.AddModelError(nameof(vm.RepositoryURL), "Enter a valid repository URL.");
            }

            NormalizeProjectMediaRows(vm);
            ValidateProjectMediaRows(vm, allowExistingMedia: false);

            if (!ModelState.IsValid)
            {
                return View(await PopulateProjectFormOptionsAsync(vm));
            }

            var mediaItems = await BuildProjectMediaItemsAsync(vm, allowExistingMedia: false);

            if (!ModelState.IsValid)
            {
                return View(await PopulateProjectFormOptionsAsync(vm));
            }

            var now = DateTime.UtcNow;

            var project = new Project
            {
                ProjectTitle = vm.ProjectTitle,
                ProjectSlug = vm.ProjectSlug,
                ShortDescription = vm.ShortDescription,
                LongDescription = vm.LongDescription,
                CategoryId = vm.CategoryId,
                RepositoryURL = vm.RepositoryURL,
                IsFeatured = vm.IsFeatured,
                CreatedOn = now
            };

            await _projectService.CreateProjectAsync(project, vm.SelectedTagIds, mediaItems);

            return RedirectToAction(nameof(Projects));
        }

        [Authorize]
        [HttpGet("/admin/projects/{id:int}/preview")]
        public async Task<IActionResult> PreviewProject(int id)
        {
            var project = await _projectService.GetAdminProjectByIdAsync(id);

            if (project == null)
            {
                return NotFound();
            }

            var vm = new AdminProjectPreviewVM
            {
                Id = project.Id,
                ProjectTitle = project.ProjectTitle,
                ProjectSlug = project.ProjectSlug,
                ProjectDescription = project.LongDescription ?? "",
                ProjectCategory = project.Category.CategoryName,
                RepositoryURL = project.RepositoryURL,
                IsFeatured = project.IsFeatured,
                Tags = project.ProjectTags
                    .Where(projectTag => projectTag.Tag != null)
                    .OrderBy(projectTag => projectTag.Tag.TagName)
                    .Select(projectTag => projectTag.Tag.TagName)
                    .ToList(),
                MediaItems = project.MediaItems
                    .OrderBy(media => media.SortOrder)
                    .Select(media => new AdminProjectMediaPreviewVM
                    {
                        MediaType = media.MediaType,
                        MediaURL = media.MediaURL,
                        ThumbnailURL = media.ThumbnailURL,
                        AltText = media.AltText ?? "",
                        SortOrder = media.SortOrder,
                        IsPrimary = media.IsPrimary
                    })
                    .ToList()
            };

            return View(vm);
        }

        [Authorize]
        [HttpGet("/admin/projects/{id:int}/edit")]
        public async Task<IActionResult> EditProject(int id)
        {
            var project = await _projectService.GetAdminProjectByIdAsync(id);

            if (project == null)
            {
                return NotFound();
            }

            var orderedMedia = project.MediaItems.OrderBy(media => media.SortOrder).ToList();

            var primaryMediaIndex = orderedMedia.FindIndex(media => media.IsPrimary);

            if (primaryMediaIndex < 0)
            {
                primaryMediaIndex = 0;
            }

            var vm = new AdminProjectFormVM
            {
                Id = project.Id,
                ProjectTitle = project.ProjectTitle,
                ProjectSlug = project.ProjectSlug,
                ShortDescription = project.ShortDescription,
                LongDescription = project.LongDescription ?? "",
                CategoryId = project.CategoryId,
                RepositoryURL = project.RepositoryURL ?? "",
                IsFeatured = project.IsFeatured,
                SelectedTagIds = project.ProjectTags.Select(projectTag => projectTag.TagId).ToList(),
                PrimaryMediaIndex = primaryMediaIndex,
                MediaItems = orderedMedia
                    .Select((media, index) => new AdminProjectMediaFormVM
                    {
                        Id = media.Id,
                        MediaType = media.MediaType,
                        YouTubeUrl = media.MediaType == "VIDEO" ? media.MediaURL : "",
                        ExistingMediaURL = media.MediaURL,
                        ExistingThumbnailURL = media.ThumbnailURL ?? "",
                        AltText = media.AltText ?? "",
                        SortOrder = index,
                        PrimaryMediaIndex = index
                    })
                    .ToList()
            };

            return View(await PopulateProjectFormOptionsAsync(vm));
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost("/admin/projects/{id:int}/edit")]
        public async Task<IActionResult> EditProject(int id, AdminProjectFormVM vm)
        {
            if (id != vm.Id)
            {
                return BadRequest();
            }

            if (await _projectService.GetAdminProjectByIdAsync(id) == null)
            {
                return NotFound();
            }

            vm.ProjectTitle = vm.ProjectTitle?.Trim() ?? "";
            vm.ProjectSlug = vm.ProjectSlug?.Trim().ToLowerInvariant() ?? "";
            vm.ShortDescription = vm.ShortDescription?.Trim() ?? "";
            vm.LongDescription = vm.LongDescription?.Trim() ?? "";
            vm.RepositoryURL = vm.RepositoryURL?.Trim() ?? "";

            if (await _projectService.ProjectSlugExistsAsync(vm.ProjectSlug, id))
            {
                ModelState.AddModelError(nameof(vm.ProjectSlug), "A project with this slug already exists.");
            }

            var categoryIds = (await _projectService.GetProjectCategoriesAsync()).Select(category => category.Id).ToHashSet();

            if (!categoryIds.Contains(vm.CategoryId))
            {
                ModelState.AddModelError(nameof(vm.CategoryId), "Select a valid category.");
            }

            NormalizeProjectMediaRows(vm);
            ValidateProjectMediaRows(vm, allowExistingMedia: true);

            if (!ModelState.IsValid)
            {
                return View(await PopulateProjectFormOptionsAsync(vm));
            }

            var mediaItems = await BuildProjectMediaItemsAsync(vm, allowExistingMedia: true);

            if (!ModelState.IsValid)
            {
                return View(await PopulateProjectFormOptionsAsync(vm));
            }

            await _projectService.UpdateProjectAsync(new Project
            {
                Id = id,
                ProjectTitle = vm.ProjectTitle,
                ProjectSlug = vm.ProjectSlug,
                ShortDescription = vm.ShortDescription,
                LongDescription = vm.LongDescription,
                CategoryId = vm.CategoryId,
                RepositoryURL = vm.RepositoryURL,
                IsFeatured = vm.IsFeatured,
                UpdatedOn = DateTime.UtcNow
            }, vm.SelectedTagIds, mediaItems);

            return RedirectToAction(nameof(Projects));
        }

        [Authorize]
        [HttpGet("/admin/projects/{id:int}/delete")]
        public async Task<IActionResult> DeleteProject(int id)
        {
            var project = await _projectService.GetProjectDeleteDetailsAsync(id);

            if (project == null)
            {
                return NotFound();
            }

            var vm = new AdminProjectDeleteVM
            {
                Id = project.Id,
                ProjectTitle = project.ProjectTitle,
                ProjectSlug = project.ProjectSlug,
                CategoryName = project.Category.CategoryName,
                IsFeatured = project.IsFeatured,
                CreatedOn = project.CreatedOn,
                UpdatedOn = project.UpdatedOn,
                TagCount = project.ProjectTags.Count,
                MediaCount = project.MediaItems.Count
            };

            return View(vm);
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost("/admin/projects/{id:int}/delete")]
        public async Task<IActionResult> DeleteProjectConfirmed(int id)
        {
            await _projectService.DeleteProjectAsync(id);

            return RedirectToAction("Projects");
        }

        // TAGS ===================================================================================

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
        public async Task<IActionResult> DeleteTagConfirmed(int id)
        {
            await _articleService.DeleteTagAsync(id);

            return RedirectToAction("Tags");
        }

        // ABOUT ==================================================================================

        [Authorize]
        [HttpGet("/admin/about")]
        public async Task<IActionResult> About()
        {
            var aboutPage = await _aboutService.GetAboutPageAsync();

            if (aboutPage == null)
            {
                return NotFound();
            }

            var vm = new AdminAboutFormVM
            {
                Id = aboutPage.Id,
                DisplayName = aboutPage.DisplayName,
                ProfileSummary = aboutPage.ProfileSummary,
                ResumePdfUrl = aboutPage.ResumePdfUrl ?? "",
                IntroductionHeading = aboutPage.IntroductionHeading,
                IntroductionMarkdown = aboutPage.IntroductionMarkdown,
                ExperienceHeading = aboutPage.ExperienceHeading,
                ExperienceMarkdown = aboutPage.ExperienceMarkdown,
                EducationHeading = aboutPage.EducationHeading,
                EducationMarkdown = aboutPage.EducationMarkdown,
                InterestsHeading = aboutPage.InterestsHeading,
                InterestsMarkdown = aboutPage.InterestsMarkdown
            };

            return View(vm);
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost("/admin/about")]
        public async Task<IActionResult> About(AdminAboutFormVM vm)
        {
            vm.DisplayName = vm.DisplayName?.Trim() ?? "";
            vm.ProfileSummary = vm.ProfileSummary?.Trim() ?? "";
            vm.ResumePdfUrl = vm.ResumePdfUrl?.Trim() ?? "";
            vm.IntroductionHeading = vm.IntroductionHeading?.Trim() ?? "";
            vm.IntroductionMarkdown = vm.IntroductionMarkdown?.Trim() ?? "";
            vm.ExperienceHeading = vm.ExperienceHeading?.Trim() ?? "";
            vm.ExperienceMarkdown = vm.ExperienceMarkdown?.Trim() ?? "";
            vm.EducationHeading = vm.EducationHeading?.Trim() ?? "";
            vm.EducationMarkdown = vm.EducationMarkdown?.Trim() ?? "";
            vm.InterestsHeading = vm.InterestsHeading?.Trim() ?? "";
            vm.InterestsMarkdown = vm.InterestsMarkdown?.Trim() ?? "";

            if (vm.ResumePdfFile != null && vm.ResumePdfFile.Length > 0)
            {
                var uploadResult = await _resumePdfUploadService.SaveResumePdfAsync(vm.ResumePdfFile);

                if (!uploadResult.Succeeded)
                {
                    ModelState.AddModelError(nameof(vm.ResumePdfFile), uploadResult.ErrorMessage);

                    return View(vm);
                }

                vm.ResumePdfUrl = uploadResult.RequestPath;
            }

            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var aboutPage = new AboutPage
            {
                Id = vm.Id,
                DisplayName = vm.DisplayName,
                ProfileSummary = vm.ProfileSummary,
                ResumePdfUrl = vm.ResumePdfUrl,
                IntroductionHeading = vm.IntroductionHeading,
                IntroductionMarkdown = vm.IntroductionMarkdown,
                ExperienceHeading = vm.ExperienceHeading,
                ExperienceMarkdown = vm.ExperienceMarkdown,
                EducationHeading = vm.EducationHeading,
                EducationMarkdown = vm.EducationMarkdown,
                InterestsHeading = vm.InterestsHeading,
                InterestsMarkdown = vm.InterestsMarkdown
            };

            if (!await _aboutService.UpdateAboutPageAsync(aboutPage))
            {
                return NotFound();
            }

            TempData["SuccessMessage"] = "About page updated.";

            return RedirectToAction("About");
        }

        // MEDIA ==================================================================================

        [Authorize]
        [HttpGet("/admin/media")]
        public async Task<IActionResult> Media(int pageNumber = 1, int pageSize = 10)
        {
            var vm = await _mediaAdminService.GetAdminMediaListAsync(pageNumber, pageSize);

            vm.SuccessMessage = TempData["SuccessMessage"] as string;
            vm.ErrorMessage = TempData["ErrorMessage"] as string;

            return View(vm);
        }

        [Authorize]
        [HttpGet("/admin/media/delete")]
        public async Task<IActionResult> DeleteMedia(string relativePath, int pageNumber = 1, int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return BadRequest();
            }

            var vm = await _mediaAdminService.GetAdminMediaDeleteDetailsAsync(relativePath);

            if (vm == null)
            {
                return NotFound();
            }

            ViewData["PageNumber"] = pageNumber;
            ViewData["PageSize"] = pageSize;

            return View(vm);
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost("/admin/media/delete")]
        public async Task<IActionResult> DeleteMediaConfirmed(string relativePath, int pageNumber = 1, int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return BadRequest();
            }

            if (!await _mediaAdminService.DeleteMediaAsync(relativePath))
            {
                TempData["ErrorMessage"] = "Media file could not be deleted. It may still be referenced or may no longer exist.";

                return RedirectToAction(nameof(Media), new
                {
                    pageNumber,
                    pageSize
                });
            }

            TempData["SuccessMessage"] = "Media file deleted.";

            return RedirectToAction(nameof(Media), new
            {
                pageNumber,
                pageSize
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
                .OrderBy(x => x.CategoryName)
                .ToList();

            vm.AvailableTags = (await _articleService.GetAllTagsAsync())
                .Select(tag => new AdminArticleTagOptionVM
                {
                    Id = tag.Id,
                    TagName = tag.TagName
                })
                .OrderBy(x => x.TagName)
                .ToList();

            return vm;
        }
        private async Task<AdminProjectFormVM> PopulateProjectFormOptionsAsync(AdminProjectFormVM vm)
        {
            vm.Categories = (await _projectService.GetProjectCategoriesAsync())
                .Select(category => new AdminProjectCategoryOptionVM
                {
                    Id = category.Id,
                    CategoryName = category.CategoryName
                })
                .OrderBy(x => x.CategoryName)
                .ToList();

            vm.AvailableTags = (await _projectService.GetAllTagsAsync())
                .Select(tag => new AdminProjectTagOptionVM
                {
                    Id = tag.Id,
                    TagName = tag.TagName
                })
                .OrderBy(x => x.TagName)
                .ToList();

            if (vm.MediaItems.Count == 0)
            {
                vm.MediaItems.Add(new AdminProjectMediaFormVM
                {
                    MediaType = "IMAGE",
                    SortOrder = 0,
                    PrimaryMediaIndex = 0
                });
            }

            return vm;
        }
        private static void NormalizeProjectMediaRows(AdminProjectFormVM vm)
        {
            vm.MediaItems = vm.MediaItems.Where(media => media.ImageFile != null 
                || !string.IsNullOrWhiteSpace(media.YouTubeUrl) 
                || !string.IsNullOrWhiteSpace(media.AltText))
                .ToList();

            for (var index = 0; index < vm.MediaItems.Count; index++)
            {
                var media = vm.MediaItems[index];

                media.MediaType = media.MediaType?.Trim().ToUpperInvariant() == "VIDEO" ? "VIDEO" : "IMAGE";
                media.YouTubeUrl = media.YouTubeUrl?.Trim() ?? "";
                media.AltText = media.AltText?.Trim() ?? "";
                media.SortOrder = index;
            }

            if (vm.PrimaryMediaIndex < 0 || vm.PrimaryMediaIndex >= vm.MediaItems.Count)
            {
                vm.PrimaryMediaIndex = 0;
            }
        }
        private void ValidateProjectMediaRows(AdminProjectFormVM vm, bool allowExistingMedia)
        {
            for (var index = 0; index < vm.MediaItems.Count; index++)
            {
                var media = vm.MediaItems[index];

                if (media.MediaType == "IMAGE" && media.ImageFile == null && (!allowExistingMedia || string.IsNullOrWhiteSpace(media.ExistingMediaURL)))
                {
                    ModelState.AddModelError($"MediaItems[{index}].ImageFile", "Choose an image or remove this media row.");
                }

                if (media.MediaType == "VIDEO" && !TryBuildYouTubeEmbedUrl(media.YouTubeUrl, out _))
                {
                    ModelState.AddModelError($"MediaItems[{index}].YouTubeUrl", "Enter a valid YouTube URL.");
                }
            }
        }
        private async Task<List<ProjectMedia>> BuildProjectMediaItemsAsync(AdminProjectFormVM vm, bool allowExistingMedia)
        {
            var mediaItems = new List<ProjectMedia>();

            for (var index = 0; index < vm.MediaItems.Count; index++)
            {
                var media = vm.MediaItems[index];

                if (media.MediaType == "IMAGE")
                {
                    if (allowExistingMedia && media.ImageFile == null && !string.IsNullOrWhiteSpace(media.ExistingMediaURL))
                    {
                        mediaItems.Add(new ProjectMedia
                        {
                            Id = media.Id ?? 0,
                            MediaType = "IMAGE",
                            MediaURL = media.ExistingMediaURL,
                            ThumbnailURL = string.IsNullOrWhiteSpace(media.ExistingThumbnailURL) ? media.ExistingMediaURL : media.ExistingThumbnailURL,
                            AltText = media.AltText,
                            SortOrder = index,
                            IsPrimary = index == vm.PrimaryMediaIndex
                        });

                        continue;
                    }

                    var uploadResult = await _projectImageUploadService.SaveProjectImageAsync(media.ImageFile!);

                    if (!uploadResult.Succeeded)
                    {
                        ModelState.AddModelError($"MediaItems[{index}].ImageFile", uploadResult.ErrorMessage);

                        continue;
                    }

                    mediaItems.Add(new ProjectMedia
                    {
                        MediaType = "IMAGE",
                        MediaURL = uploadResult.RequestPath,
                        ThumbnailURL = uploadResult.RequestPath,
                        AltText = media.AltText,
                        SortOrder = index,
                        IsPrimary = index == vm.PrimaryMediaIndex
                    });
                }
                else if (TryBuildYouTubeEmbedUrl(media.YouTubeUrl, out var embedUrl))
                {
                    mediaItems.Add(new ProjectMedia
                    {
                        MediaType = "VIDEO",
                        MediaURL = embedUrl,
                        ThumbnailURL = BuildYouTubeThumbnailUrl(embedUrl),
                        AltText = media.AltText,
                        SortOrder = index,
                        IsPrimary = index == vm.PrimaryMediaIndex
                    });
                }
            }

            if (mediaItems.Count > 0 && !mediaItems.Any(media => media.IsPrimary))
            {
                mediaItems[0].IsPrimary = true;
            }

            return mediaItems;
        }
        private static bool TryBuildYouTubeEmbedUrl(string? url, out string embedUrl)
        {
            embedUrl = "";

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            {
                return false;
            }

            var host = uri.Host.ToLowerInvariant();

            string? videoId = null;

            if (host == "youtu.be")
            {
                videoId = uri.AbsolutePath.Trim('/').Split('/').FirstOrDefault();
            }
            else if (host is "youtube.com" or "www.youtube.com" or "m.youtube.com")
            {
                if (uri.AbsolutePath.Equals("/watch", StringComparison.OrdinalIgnoreCase))
                {
                    var query = QueryHelpers.ParseQuery(uri.Query);

                    if (query.TryGetValue("v", out var value))
                    {
                        videoId = value.FirstOrDefault();
                    }
                }
                else
                {
                    var pathParts = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

                    if (pathParts.Length >= 2 && pathParts[0] is "embed" or "shorts" or "live")
                    {
                        videoId = pathParts[1];
                    }
                }
            }
            else if (host is "youtube-nocookie.com" or "www.youtube-nocookie.com")
            {
                var pathParts = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

                if (pathParts.Length >= 2 && pathParts[0] == "embed")
                {
                    videoId = pathParts[1];
                }
            }

            if (string.IsNullOrWhiteSpace(videoId) || !Regex.IsMatch(videoId, "^[A-Za-z0-9_-]{11}$"))
            {
                return false;
            }

            embedUrl = $"https://www.youtube-nocookie.com/embed/{videoId}";
            return true;
        }
        private static string BuildYouTubeThumbnailUrl(string embedUrl)
        {
            var videoId = embedUrl
                .Split("/embed/", StringSplitOptions.RemoveEmptyEntries)
                .LastOrDefault()?
                .Split("?", StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(videoId))
            {
                return "";
            }

            return $"https://img.youtube.com/vi/{videoId}/hqdefault.jpg";
        }
        private void SetPendingMfaCookie(string username, string? returnUrl)
        {
            var expiresUtc = DateTimeOffset.UtcNow.AddMinutes(5);

            var payload = string.Join(".",
                Microsoft.AspNetCore.Authentication.Base64UrlTextEncoder.Encode(Encoding.UTF8.GetBytes(username)),
                Microsoft.AspNetCore.Authentication.Base64UrlTextEncoder.Encode(Encoding.UTF8.GetBytes(returnUrl ?? "")),
                expiresUtc.ToUnixTimeSeconds());

            Response.Cookies.Append(
                PendingMfaCookieName,
                _pendingMfaProtector.Protect(payload),
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Path = "/admin",
                    Expires = expiresUtc,
                    IsEssential = true
                });
        }
        private bool TryGetPendingMfa(out string username, out string? returnUrl)
        {
            username = "";
            returnUrl = null;

            if (!Request.Cookies.TryGetValue(PendingMfaCookieName, out var protectedPayload) ||
                string.IsNullOrWhiteSpace(protectedPayload))
            {
                return false;
            }

            try
            {
                var payload = _pendingMfaProtector.Unprotect(protectedPayload);
                var payloadParts = payload.Split('.', 3);

                if (payloadParts.Length != 3)
                {
                    return false;
                }

                if (!long.TryParse(payloadParts[2], out var expiresUnixSeconds) ||
                    expiresUnixSeconds <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                {
                    return false;
                }

                username = Encoding.UTF8.GetString(Microsoft.AspNetCore.Authentication.Base64UrlTextEncoder.Decode(payloadParts[0]));
                returnUrl = Encoding.UTF8.GetString(Microsoft.AspNetCore.Authentication.Base64UrlTextEncoder.Decode(payloadParts[1]));

                if (string.IsNullOrWhiteSpace(username))
                {
                    username = "";
                    returnUrl = null;

                    return false;
                }

                if (string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl))
                {
                    returnUrl = null;
                }

                return true;
            }
            catch (CryptographicException)
            {
                return false;
            }
            catch (FormatException)
            {
                return false;
            }
        }
        private void ClearPendingMfaCookie()
        {
            Response.Cookies.Delete(PendingMfaCookieName, new CookieOptions
            {
                Path = "/admin"
            });
        }
    }
}
