using mcnylo.dev.About.ViewModels;
using mcnylo.dev.Data.Models;

namespace mcnylo.dev.About.Services
{
    public interface IAboutService
    {
        public Task<AboutPage?> GetAboutPageAsync();
        public Task<bool> UpdateAboutPageAsync(AboutPage aboutPage);
        public Task<AboutPageVM?> GetAboutPageViewModelAsync();
    }
}
