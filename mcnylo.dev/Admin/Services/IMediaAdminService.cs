using mcnylo.dev.Admin.ViewModels.Media;

namespace mcnylo.dev.Admin.Services
{
    public interface IMediaAdminService
    {
        public Task<AdminMediaListVM> GetAdminMediaListAsync(int pageNumber, int pageSize);
        public Task<AdminMediaDeleteVM?> GetAdminMediaDeleteDetailsAsync(string relativePath);
        public Task<bool> DeleteMediaAsync(string relativePath);
    }
}
