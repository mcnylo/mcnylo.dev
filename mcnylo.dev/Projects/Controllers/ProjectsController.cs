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

        [HttpGet("projects")]
        public async Task<IActionResult> Index()
        {
            var vm = await _projectService.BuildProjectIndexVM(new ProjectFilterVM());

            return View(vm);
        }

        [HttpGet("projects/search")]
        public async Task<IActionResult> Search([FromQuery] ProjectFilterVM filter)
        {
            var projects = await _projectService.GetProjectResults(filter);

            return PartialView("_ProjectCards", projects);
        }

        [HttpGet("projects/{slug}")]
        public async Task<IActionResult> Details(string slug)
        {
            ProjectDetailsVM? vm = await _projectService.GetProjectDetailsBySlug(slug);

            if (vm == null)
            {
                return NotFound();
            }

            return View(vm);
        }
    }
}
