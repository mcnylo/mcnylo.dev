using mcnylo.dev.Projects.Services;
using mcnylo.dev.Projects.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace mcnylo.dev.Projects.Controllers
{
    public class ProjectsController : Controller
    {
        private readonly IProjectService _projectService;

        // ========================================================================================

        public ProjectsController(IProjectService projectService)
        {
            _projectService = projectService;
        }

        // ========================================================================================

        public async Task<IActionResult> Index()
        {
            var vm = await _projectService.BuildProjectIndexVM(new ProjectFilterVM());

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] ProjectFilterVM filter)
        {
            var projects = await _projectService.GetProjectCards(filter);

            return PartialView("_ProjectCards", projects);
        }
    }
}
