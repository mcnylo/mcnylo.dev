using mcnylo.dev.Data.Models;

namespace mcnylo.dev.Home.Models
{
    public class FeaturedProject
    {
        public string ProjectName { get; set; } = "";
        public string ProjectShortDescription { get; set; } = "";
        public string ProjectCategory { get; set; } = "";
        public int YearCreated { get; set; } = 2000;
        public List<ProjectTag> ProjectTags { get; set; } = new List<ProjectTag>();
    }
}
