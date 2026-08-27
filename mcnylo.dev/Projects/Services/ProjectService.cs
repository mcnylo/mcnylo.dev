using mcnylo.dev.Admin.ViewModels.Projects;
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

                p.ProjectThumbnailURL = primaryMedia != null ? primaryMedia.ThumbnailURL! : "/images/thumb-placeholder.jpg";
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
        public async Task<AdminProjectListVM> GetAdminProjectResultsAsync(string? search, int pageNumber, int pageSize)
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

            var query = _dbContext.Projects.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                query = query.Where(project => project.ProjectTitle.Contains(normalizedSearch) || project.ProjectSlug.Contains(normalizedSearch));
            }

            var totalProjects = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalProjects / (double)pageSize);

            if (totalPages > 0 && pageNumber > totalPages)
            {
                pageNumber = totalPages;
            }

            var projects = await query
                .OrderByDescending(project => project.UpdatedOn ?? project.CreatedOn)
                .ThenByDescending(project => project.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(project => new AdminProjectListItemVM
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
                })
                .ToListAsync();

            return new AdminProjectListVM
            {
                Search = normalizedSearch,
                Projects = projects,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalProjects = totalProjects,
                TotalPages = totalPages
            };
        }
        public async Task<AdminProjectCategoryListVM> GetAdminProjectCategoryResultsAsync(int pageNumber, int pageSize)
        {
            if (pageNumber < 1)
            {
                pageNumber = 1;
            }

            if (pageSize < 1)
            {
                pageSize = 10;
            }

            var query = _dbContext.ProjectCategories.AsNoTracking();

            var totalCategories = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCategories / (double)pageSize);

            if (totalPages > 0 && pageNumber > totalPages)
            {
                pageNumber = totalPages;
            }

            var categories = await query
                .OrderBy(category => category.CategoryName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(category => new AdminProjectCategoryListItemVM
                {
                    Id = category.Id,
                    CategoryName = category.CategoryName,
                    CategorySlug = category.CategorySlug,
                    ProjectCount = category.Projects.Count
                })
                .ToListAsync();

            return new AdminProjectCategoryListVM
            {
                Categories = categories,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCategories = totalCategories,
                TotalPages = totalPages
            };
        }
        public async Task<bool> ProjectCategorySlugExistsAsync(string slug, int? excludedCategoryId = null)
        {
            return await _dbContext.ProjectCategories.AsNoTracking()
                .AnyAsync(category => category.CategorySlug == slug && (!excludedCategoryId.HasValue || category.Id != excludedCategoryId.Value));
        }
        public async Task<int> CreateProjectCategoryAsync(ProjectCategory category)
        {
            _dbContext.ProjectCategories.Add(category);

            await _dbContext.SaveChangesAsync();

            return category.Id;
        }
        public async Task<ProjectCategory?> GetProjectCategoryByIdAsync(int id)
        {
            return await _dbContext.ProjectCategories.FirstOrDefaultAsync(category => category.Id == id);
        }
        public async Task UpdateProjectCategoryAsync(ProjectCategory category)
        {
            await _dbContext.SaveChangesAsync();
        }
        public async Task<ProjectCategory?> GetProjectCategoryDeleteDetailsAsync(int id)
        {
            return await _dbContext.ProjectCategories.AsNoTracking()
                .Include(category => category.Projects)
                .FirstOrDefaultAsync(category => category.Id == id);
        }
        public async Task DeleteProjectCategoryAsync(int id)
        {
            var category = await _dbContext.ProjectCategories
                .Include(category => category.Projects)
                .FirstOrDefaultAsync(category => category.Id == id);

            if (category == null || category.Projects.Any())
            {
                return;
            }

            _dbContext.ProjectCategories.Remove(category);

            await _dbContext.SaveChangesAsync();
        }
        public async Task<List<ProjectCategory>> GetProjectCategoriesAsync()
        {
            return await _dbContext.ProjectCategories.AsNoTracking()
                .OrderBy(category => category.CategoryName)
                .ToListAsync();
        }
        public async Task<List<Tag>> GetAllTagsAsync()
        {
            return await _dbContext.Tags.AsNoTracking()
                .OrderBy(tag => tag.TagName)
                .ToListAsync();
        }
        public async Task<bool> ProjectSlugExistsAsync(string slug, int? excludedProjectId = null)
        {
            return await _dbContext.Projects.AsNoTracking()
                .AnyAsync(project => project.ProjectSlug == slug && (!excludedProjectId.HasValue || project.Id != excludedProjectId.Value));
        }
        public async Task<int> CreateProjectAsync(Project project, List<int> tagIds, List<ProjectMedia> mediaItems)
        {
            var selectedTagIds = tagIds.Distinct().ToList();

            var validTagIds = await _dbContext.Tags.AsNoTracking()
                .Where(tag => selectedTagIds.Contains(tag.Id))
                .Select(tag => tag.Id)
                .ToListAsync();

            project.ProjectTags = validTagIds
                .Select(tagId => new ProjectTag
                {
                    TagId = tagId
                })
                .ToList();

            project.MediaItems = mediaItems.OrderBy(media => media.SortOrder).ToList();

            _dbContext.Projects.Add(project);

            await _dbContext.SaveChangesAsync();

            return project.Id;
        }
        public async Task<Project?> GetAdminProjectByIdAsync(int id)
        {
            return await _dbContext.Projects
                .AsNoTracking()
                .Include(project => project.Category)
                .Include(project => project.ProjectTags)
                    .ThenInclude(projectTag => projectTag.Tag)
                .Include(project => project.MediaItems)
                .FirstOrDefaultAsync(project => project.Id == id);
        }
        public async Task UpdateProjectAsync(Project project, List<int> tagIds, List<ProjectMedia> mediaItems)
        {
            var existingProject = await _dbContext.Projects
                .Include(project => project.ProjectTags)
                .Include(project => project.MediaItems)
                .FirstOrDefaultAsync(existingProject => existingProject.Id == project.Id);

            if (existingProject == null)
            {
                return;
            }

            existingProject.ProjectTitle = project.ProjectTitle;
            existingProject.ProjectSlug = project.ProjectSlug;
            existingProject.ShortDescription = project.ShortDescription;
            existingProject.LongDescription = project.LongDescription;
            existingProject.CategoryId = project.CategoryId;
            existingProject.RepositoryURL = project.RepositoryURL;
            existingProject.IsFeatured = project.IsFeatured;
            existingProject.UpdatedOn = project.UpdatedOn;

            var selectedTagIds = tagIds.Distinct().ToList();

            var validTagIds = await _dbContext.Tags.AsNoTracking()
                .Where(tag => selectedTagIds.Contains(tag.Id))
                .Select(tag => tag.Id)
                .ToListAsync();

            var validTagIdSet = validTagIds.ToHashSet();
            var existingTagIds = existingProject.ProjectTags.Select(projectTag => projectTag.TagId).ToHashSet();

            var tagsToRemove = existingProject.ProjectTags
                .Where(projectTag => !validTagIdSet.Contains(projectTag.TagId))
                .ToList();

            _dbContext.ProjectTags.RemoveRange(tagsToRemove);

            foreach (var tagId in validTagIds.Where(tagId => !existingTagIds.Contains(tagId)))
            {
                _dbContext.ProjectTags.Add(new ProjectTag
                {
                    ProjectId = existingProject.Id,
                    TagId = tagId
                });
            }

            var submittedExistingMediaIds = mediaItems
                .Where(media => media.Id > 0)
                .Select(media => media.Id)
                .ToHashSet();

            var mediaToRemove = existingProject.MediaItems
                .Where(media => !submittedExistingMediaIds.Contains(media.Id))
                .ToList();

            _dbContext.ProjectMedia.RemoveRange(mediaToRemove);

            foreach (var media in mediaItems.OrderBy(media => media.SortOrder))
            {
                if (media.Id > 0)
                {
                    var existingMedia = existingProject.MediaItems.FirstOrDefault(projectMedia => projectMedia.Id == media.Id);

                    if (existingMedia == null)
                    {
                        continue;
                    }

                    existingMedia.MediaType = media.MediaType;
                    existingMedia.MediaURL = media.MediaURL;
                    existingMedia.ThumbnailURL = media.ThumbnailURL;
                    existingMedia.AltText = media.AltText;
                    existingMedia.SortOrder = media.SortOrder;
                    existingMedia.IsPrimary = media.IsPrimary;

                    continue;
                }

                _dbContext.ProjectMedia.Add(new ProjectMedia
                {
                    ProjectId = existingProject.Id,
                    MediaType = media.MediaType,
                    MediaURL = media.MediaURL,
                    ThumbnailURL = media.ThumbnailURL,
                    AltText = media.AltText,
                    SortOrder = media.SortOrder,
                    IsPrimary = media.IsPrimary
                });
            }

            await _dbContext.SaveChangesAsync();
        }
        public async Task<Project?> GetProjectDeleteDetailsAsync(int id)
        {
            return await _dbContext.Projects
                .AsNoTracking()
                .Include(project => project.Category)
                .Include(project => project.ProjectTags)
                .Include(project => project.MediaItems)
                .FirstOrDefaultAsync(project => project.Id == id);
        }

        public async Task DeleteProjectAsync(int id)
        {
            var project = await _dbContext.Projects
                .Include(project => project.ProjectTags)
                .Include(project => project.MediaItems)
                .FirstOrDefaultAsync(project => project.Id == id);

            if (project == null)
            {
                return;
            }

            _dbContext.ProjectTags.RemoveRange(project.ProjectTags);
            _dbContext.ProjectMedia.RemoveRange(project.MediaItems);
            _dbContext.Projects.Remove(project);

            await _dbContext.SaveChangesAsync();
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
