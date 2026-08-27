using mcnylo.dev.About.Services;
using Microsoft.AspNetCore.Mvc;

namespace mcnylo.dev.About.Controllers
{
    public class AboutController : Controller
    {
        private readonly IAboutService _aboutService;

        // ========================================================================================

        public AboutController(IAboutService aboutService)
        {
            _aboutService = aboutService;
        }

        // ========================================================================================

        public async Task<IActionResult> Index()
        {
            var vm = await _aboutService.GetAboutPageViewModelAsync();

            if (vm == null)
            {
                return NotFound();
            }

            return View(vm);
        }
    }
}
