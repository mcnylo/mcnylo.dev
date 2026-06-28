using mcnylo.dev.Home.ViewModels;

namespace mcnylo.dev.Home.Services
{
    public interface IHomeService
    {
        public Task<HomeVM> BuildHomeVM();
    }
}
