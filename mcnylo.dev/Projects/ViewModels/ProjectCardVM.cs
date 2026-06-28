namespace mcnylo.dev.Projects.ViewModels
{
    public class ProjectCardVM
    {
        public string ProjectTitle { get; set; } = "";
        public string ProjectSlug { get; set; } = "";
        public string ProjectShortDescription { get; set; } = "";
        public string ProjectCategory { get; set; } = "";
        public List<string> Tags { get; set; } = new List<string>();
    }
}
