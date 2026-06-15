using Microsoft.AspNetCore.Mvc;

namespace mcnylo.dev.Home.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
