using mcnylo.dev.Admin.ViewModels.Projects;
using mcnylo.dev.Data.Models;
using mcnylo.dev.Projects.ViewModels;

namespace mcnylo.dev.Projects.Services
{
    public interface IProjectService
    {
        public Task<ProjectIndexVM> BuildProjectIndexVM(ProjectFilterVM filter);
        public Task<ProjectResultsVM> GetProjectResults(ProjectFilterVM filter);
        public Task<ProjectDetailsVM?> GetProjectDetailsBySlug(string slug);
        public Task<AdminProjectListVM> GetAdminProjectResultsAsync(string? search, int pageNumber, int pageSize);
        public Task<AdminProjectCategoryListVM> GetAdminProjectCategoryResultsAsync(int pageNumber, int pageSize);
        public Task<bool> ProjectCategorySlugExistsAsync(string slug, int? excludedCategoryId = null);
        public Task<int> CreateProjectCategoryAsync(ProjectCategory category);
        public Task<ProjectCategory?> GetProjectCategoryByIdAsync(int id);
        public Task UpdateProjectCategoryAsync(ProjectCategory category);
        public Task<ProjectCategory?> GetProjectCategoryDeleteDetailsAsync(int id);
        public Task DeleteProjectCategoryAsync(int id);
        public Task<List<ProjectCategory>> GetProjectCategoriesAsync();
        public Task<List<Tag>> GetAllTagsAsync();
        public Task<bool> ProjectSlugExistsAsync(string slug, int? excludedProjectId = null);
        public Task<int> CreateProjectAsync(Project project, List<int> tagIds, List<ProjectMedia> mediaItems);
        public Task<Project?> GetAdminProjectByIdAsync(int id);
        public Task UpdateProjectAsync(Project project, List<int> tagIds, List<ProjectMedia> mediaItems);
        public Task<Project?> GetProjectDeleteDetailsAsync(int id);
        public Task DeleteProjectAsync(int id);
    }
}
