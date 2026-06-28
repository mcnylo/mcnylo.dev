using mcnylo.dev.Home.Services;
using Microsoft.AspNetCore.Mvc;

namespace mcnylo.dev.Home.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHomeService _homeService;

        // ========================================================================================

        public HomeController(IHomeService homeService)
        {
            _homeService = homeService;
        }

        // ========================================================================================

        public async Task<IActionResult> Index()
        {
            var vm = await _homeService.BuildHomeVM();

            return View(vm);
        }
    }
}
