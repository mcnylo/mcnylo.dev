using mcnylo.dev.Projects.ViewModels;

namespace mcnylo.dev.Projects.Services
{
    public interface IProjectService
    {
        public Task<ProjectIndexVM> BuildProjectIndexVM(ProjectFilterVM filter);
        public Task<ProjectResultsVM> GetProjectResults(ProjectFilterVM filter);
        public Task<ProjectDetailsVM?> GetProjectDetailsBySlug(string slug);
    }
}
