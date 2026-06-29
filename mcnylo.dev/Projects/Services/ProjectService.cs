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
            vm.Projects = await GetProjectResults(filter);
            vm.Categories = await GetCategoryOptions();
            vm.Tags = await GetTagOptions();

            return vm;
        }
        public async Task<ProjectResultsVM> GetProjectResults(ProjectFilterVM filter)
        {
            ProjectResultsVM vm = new ProjectResultsVM();

            if (filter.PageNumber < 1)
            {
                filter.PageNumber = 1;
            }
            else
            {
                filter.PageNumber = filter.PageNumber;
            }

            if (filter.PageSize < 1)
            {
                filter.PageSize = 5;
            }
            else
            {
                filter.PageSize = filter.PageSize;
            }

            IQueryable<Project> query = _dbContext.Projects
                .AsNoTracking()
                .Include(project => project.Category)
                .Include(project => project.ProjectTags)
                    .ThenInclude(projectTag => projectTag.Tag)
                .Include(project => project.MediaItems);

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

            if (selectedTagSlugs.Count > 0)
            {
                query = query.Where(project => project.ProjectTags.Any(projectTag => selectedTagSlugs.Contains(projectTag.Tag.TagSlug)));
            }

            int totalProjects = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalProjects / (double)filter.PageSize);

            if (totalPages > 0 && filter.PageNumber > totalPages)
            {
                filter.PageNumber = totalPages;
            }

            var projects = await query
                .OrderBy(project => project.ProjectTitle)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            List<ProjectCardVM> projectCards = new List<ProjectCardVM>();

            foreach (var project in projects)
            {
                ProjectCardVM p = new ProjectCardVM();

                p.ProjectTitle = project.ProjectTitle;
                p.ProjectSlug = project.ProjectSlug;
                p.ProjectShortDescription = project.ShortDescription;
                p.ProjectCategory = project.Category.CategoryName;
                p.IsFeatured = project.IsFeatured;

                var tags = project.ProjectTags.Select(x => x.Tag.TagName).OrderBy(tagName => tagName).ToList();
                p.Tags = tags;

                var primaryMedia = project.MediaItems.Where(x => x.IsPrimary).FirstOrDefault();

                p.ProjectThumbnailURL = primaryMedia != null ? primaryMedia.ThumbnailURL! : "/images/project-thumb-placeholder.jpg";
                p.ProjectThumbnailAltText = primaryMedia != null ? primaryMedia.AltText! : "No image available for this project.";

                projectCards.Add(p);
            }

            vm.Projects = projectCards;
            vm.PageNumber = filter.PageNumber;
            vm.PageSize = filter.PageSize;
            vm.TotalProjects = totalProjects;
            vm.TotalPages = totalPages;

            return vm;
        }
        public async Task<ProjectDetailsVM?> GetProjectDetailsBySlug(string slug)
        {
            if (string.IsNullOrEmpty(slug))
            {
                return null;
            }

            var project = await _dbContext.Projects
                .AsNoTracking()
                .Include(project => project.Category)
                .Include(project => project.ProjectTags)
                    .ThenInclude(projectTag => projectTag.Tag)
                .Include(project => project.MediaItems)
                .Where(project => project.ProjectSlug == slug)
                .FirstOrDefaultAsync();

            if (project == null)
            {
                return null;
            }

            ProjectDetailsVM vm = new ProjectDetailsVM();

            vm.ProjectTitle = project.ProjectTitle;
            vm.ProjectSlug = project.ProjectSlug;
            vm.ProjectDescription = project.LongDescription ?? "";
            vm.ProjectCategory = project.Category.CategoryName;
            vm.RepositoryURL = project.RepositoryURL;
            vm.IsFeatured = project.IsFeatured;

            var tags = project.ProjectTags.Select(x => x.Tag.TagName).OrderBy(tagName => tagName).ToList();

            vm.Tags = tags;

            List<ProjectMediaVM> projectMedia = new List<ProjectMediaVM>();

            var mediaItems = project.MediaItems.OrderBy(x => x.SortOrder).ToList();

            foreach (var media in mediaItems)
            {
                ProjectMediaVM m = new ProjectMediaVM();

                m.MediaType = media.MediaType;
                m.MediaURL = media.MediaURL;
                m.ThumbnailURL = media.ThumbnailURL;
                m.AltText = media.AltText ?? "";
                m.SortOrder = media.SortOrder;
                m.IsPrimary = media.IsPrimary;

                projectMedia.Add(m);
            }

            vm.MediaItems = projectMedia;

            return vm;
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
