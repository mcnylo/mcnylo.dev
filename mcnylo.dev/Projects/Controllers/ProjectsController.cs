using Microsoft.AspNetCore.Mvc;

namespace mcnylo.dev.Projects.Controllers
{
    public class ProjectsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
