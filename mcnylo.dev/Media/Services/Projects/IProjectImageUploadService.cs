namespace mcnylo.dev.Media.Services.Projects
{
    public interface IProjectImageUploadService
    {
        Task<ProjectImageUploadResult> SaveProjectImageAsync(IFormFile imageFile, CancellationToken cancellationToken = default);
    }
}
