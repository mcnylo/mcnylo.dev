using mcnylo.dev.Data.Context;
using mcnylo.dev.Home.Models;
using mcnylo.dev.Home.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace mcnylo.dev.Home.Services
{
    public class HomeService : IHomeService
    {
        private readonly McNyloDbContext _dbContext;

        // ========================================================================================

        public HomeService(McNyloDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // ========================================================================================

        public async Task<HomeVM> BuildHomeVM()
        {
            HomeVM vm = new HomeVM();

            List<FeaturedProject> featuredProjects = new List<FeaturedProject>();

            var projects = await _dbContext.Projects
                .Include(project => project.Category)
                .Include(project => project.ProjectTags)
                    .ThenInclude(projectTag => projectTag.Tag)
                .Where(project => project.IsFeatured)
                .OrderBy(project => project.ProjectTitle)
                .ToListAsync();

            foreach (var project in projects)
            {
                FeaturedProject p = new FeaturedProject();

                p.ProjectName = project.ProjectTitle;
                p.ProjectShortDescription = project.ShortDescription;
                p.ProjectCategory = project.Category.CategoryName;
                p.YearCreated = project.CreatedOn.Year;
                p.ProjectTags = project.ProjectTags.ToList();

                featuredProjects.Add(p);
            }

            vm.FeaturedProjects = featuredProjects;

            return vm;
        }
    }
}
