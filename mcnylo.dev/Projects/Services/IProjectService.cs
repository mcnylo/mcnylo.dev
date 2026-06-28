using mcnylo.dev.Projects.ViewModels;

namespace mcnylo.dev.Projects.Services
{
    public interface IProjectService
    {
        public Task<ProjectIndexVM> BuildProjectIndexVM(ProjectFilterVM filter);
        public Task<List<ProjectCardVM>> GetProjectCards(ProjectFilterVM filter);
    }
}
