using mcnylo.dev.Data.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mcnylo.dev.Projects.Controllers
{
    public class ProjectsController : Controller
    {
        

        public ProjectsController()
        {
            
        }

        public async Task<IActionResult> Index()
        {
            return View();
        }
    }
}
