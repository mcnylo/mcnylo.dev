using mcnylo.dev.Data.Models;

namespace mcnylo.dev.Home.ViewModels
{
    public class FeaturedProjectVM
    {
        public string ProjectName { get; set; } = "";
        public string ProjectShortDescription { get; set; } = "";
        public string ProjectCategory { get; set; } = "";
        public string ProjectSlug { get; set; } = "";
        public int YearCreated { get; set; } = 2000;
        public List<ProjectTag> ProjectTags { get; set; } = new List<ProjectTag>();
    }
}
