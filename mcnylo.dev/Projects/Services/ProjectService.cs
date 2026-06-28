using mcnylo.dev.Data.Context;
using mcnylo.dev.Data.Models;
using mcnylo.dev.Projects.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace mcnylo.dev.Projects.Services
{
    public class ProjectService : IProjectService
    {
        private readonly McNyloDbContext _dbContext;

        // ========================================================================================

        public ProjectService(McNyloDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // ========================================================================================

        public async Task<ProjectIndexVM> BuildProjectIndexVM(ProjectFilterVM filter)
        {
            ProjectIndexVM vm = new ProjectIndexVM();

            vm.Filter = filter;
            vm.Projects = await GetProjectCards(filter);
            vm.Categories = await GetCategoryOptions();
            vm.Tags = await GetTagOptions();

            return vm;
        }
        public async Task<List<ProjectCardVM>> GetProjectCards(ProjectFilterVM filter)
        {
            List<ProjectCardVM> projectCards = new List<ProjectCardVM>();

            IQueryable<Project> query = _dbContext.Projects
                .AsNoTracking()
                .Include(project => project.Category)
                .Include(project => project.ProjectTags)
                    .ThenInclude(projectTag => projectTag.Tag);

            // Search filter
            if (!string.IsNullOrEmpty(filter.Search))
            {
                var search = filter.Search.Trim();

                query = query.Where(x => x.ProjectTitle.Contains(search));
            }

            // Category filter
            var selectedCategorySlugs = filter.CategorySlugs.Where(slug => !string.IsNullOrEmpty(slug)).Distinct().ToList();

            if (selectedCategorySlugs.Count > 0)
            {
                query = query.Where(x => selectedCategorySlugs.Contains(x.Category.CategorySlug));
            }

            // Tag filter
            var selectedTagSlugs = filter.TagSlugs.Where(slug => !string.IsNullOrEmpty(slug)).Distinct().ToList();

            foreach (var tag in selectedTagSlugs)
            {
                query = query.Where(project => project.ProjectTags.Any(projectTag => projectTag.Tag.TagSlug == tag));
            }

            var projects = await query.OrderByDescending(x => x.CreatedOn).ToListAsync();

            foreach (var project in projects)
            {
                ProjectCardVM p = new ProjectCardVM();

                p.ProjectTitle = project.ProjectTitle;
                p.ProjectSlug = project.ProjectSlug;
                p.ProjectShortDescription = project.ShortDescription;
                p.ProjectCategory = project.Category.CategoryName;

                var tags = project.ProjectTags.Select(x => x.Tag.TagName).OrderBy(tagName => tagName).ToList();

                p.Tags = tags;

                projectCards.Add(p);
            }

            return projectCards;
        }

        // ========================================================================================

        private async Task<List<FilterOptionVM>> GetCategoryOptions()
        {
            var categories = await _dbContext.ProjectCategories
                .AsNoTracking()
                .OrderBy(x => x.CategoryName)
                .Select(x => new FilterOptionVM
                {
                    Name = x.CategoryName,
                    Slug = x.CategorySlug
                })
                .ToListAsync();

            return categories;
        }
        private async Task<List<FilterOptionVM>> GetTagOptions()
        {
            var tags = await _dbContext.Tags
                .AsNoTracking()
                .OrderBy(x => x.TagName)
                .Select(x => new FilterOptionVM
                {
                    Name = x.TagName,
                    Slug = x.TagSlug
                })
                .ToListAsync();

            return tags;
        }
    }
}
