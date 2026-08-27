namespace mcnylo.dev.Media.Services.Resume
{
    public interface IResumePdfUploadService
    {
        public Task<ResumePdfUploadResult> SaveResumePdfAsync(IFormFile resumePdfFile);
    }
}
