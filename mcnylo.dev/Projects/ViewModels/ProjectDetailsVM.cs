namespace mcnylo.dev.Projects.ViewModels
{
    public class ProjectDetailsVM
    {
        public string ProjectTitle { get; set; } = "";
        public string ProjectSlug { get; set; } = "";
        public string ProjectDescription { get; set; } = "";
        public string ProjectCategory { get; set; } = "";
        public string? RepositoryURL { get; set; }
        public bool IsFeatured { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public List<ProjectMediaVM> MediaItems { get; set; } = new List<ProjectMediaVM>();
    }
}
