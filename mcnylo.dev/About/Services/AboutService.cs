using mcnylo.dev.About.ViewModels;
using mcnylo.dev.Articles.Services;
using mcnylo.dev.Data.Context;
using mcnylo.dev.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace mcnylo.dev.About.Services
{
    public class AboutService : IAboutService
    {
        private readonly McNyloDbContext _dbContext;
        private readonly IArticleMarkdownService _markdownService;

        // ========================================================================================

        public AboutService(McNyloDbContext dbContext, IArticleMarkdownService markdownService)
        {
            _dbContext = dbContext;
            _markdownService = markdownService;
        }

        // ========================================================================================

        public async Task<AboutPage?> GetAboutPageAsync()
        {
            return await _dbContext.AboutPages.AsNoTracking().OrderBy(aboutPage => aboutPage.Id).FirstOrDefaultAsync();
        }
        public async Task<bool> UpdateAboutPageAsync(AboutPage aboutPage)
        {
            var existingAboutPage = await _dbContext.AboutPages.FirstOrDefaultAsync(existingAboutPage => existingAboutPage.Id == aboutPage.Id);

            if (existingAboutPage == null)
            {
                return false;
            }

            existingAboutPage.DisplayName = aboutPage.DisplayName;
            existingAboutPage.ProfileSummary = aboutPage.ProfileSummary;
            existingAboutPage.ResumePdfUrl = aboutPage.ResumePdfUrl;
            existingAboutPage.IntroductionHeading = aboutPage.IntroductionHeading;
            existingAboutPage.IntroductionMarkdown = aboutPage.IntroductionMarkdown;
            existingAboutPage.ExperienceHeading = aboutPage.ExperienceHeading;
            existingAboutPage.ExperienceMarkdown = aboutPage.ExperienceMarkdown;
            existingAboutPage.EducationHeading = aboutPage.EducationHeading;
            existingAboutPage.EducationMarkdown = aboutPage.EducationMarkdown;
            existingAboutPage.InterestsHeading = aboutPage.InterestsHeading;
            existingAboutPage.InterestsMarkdown = aboutPage.InterestsMarkdown;
            existingAboutPage.UpdatedOn = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            return true;
        }
        public async Task<AboutPageVM?> GetAboutPageViewModelAsync()
        {
            var aboutPage = await GetAboutPageAsync();

            if (aboutPage == null)
            {
                return null;
            }

            return new AboutPageVM
            {
                DisplayName = aboutPage.DisplayName,
                ProfileSummary = aboutPage.ProfileSummary,
                ResumePdfUrl = aboutPage.ResumePdfUrl,
                Sections =
                [
                    BuildSection("introduction", "Introduction", "01", aboutPage.IntroductionHeading, aboutPage.IntroductionMarkdown, true),
                    BuildSection("experience", "Experience", "02", aboutPage.ExperienceHeading, aboutPage.ExperienceMarkdown, true),
                    BuildSection("education", "Education", "03", aboutPage.EducationHeading, aboutPage.EducationMarkdown, true),
                    BuildSection("interests", "Interests", "04", aboutPage.InterestsHeading, aboutPage.InterestsMarkdown, false)
                ]
            };
        }

        // ========================================================================================

        private AboutSectionVM BuildSection(string anchorId, string navigationLabel, string navigationIndex, string? heading, string? markdownContent, bool hasDividerAfter)
        {
            return new AboutSectionVM
            {
                AnchorId = anchorId,
                NavigationLabel = navigationLabel,
                NavigationIndex = navigationIndex,
                Heading = heading ?? "",
                HtmlContent = _markdownService.RenderToHtml(markdownContent ?? ""),
                HasDividerAfter = hasDividerAfter
            };
        }
    }
}
