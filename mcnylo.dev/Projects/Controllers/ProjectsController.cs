using mcnylo.dev.Data.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mcnylo.dev.Projects.Controllers
{
    public class ProjectsController : Controller
    {
        private readonly McNyloDbContext _context;

        public ProjectsController(McNyloDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var projects = await _context.Projects
                .Include(project => project.Category)
                .Include(project => project.ProjectTags)
                    .ThenInclude(projectTag => projectTag.Tag)
                .OrderBy(project => project.ProjectTitle)
                .ToListAsync();

            return View(projects);
        }
    }
}
