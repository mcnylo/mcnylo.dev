namespace mcnylo.dev.Projects.ViewModels
{
    public class ProjectCardVM
    {
        public string ProjectTitle { get; set; } = "";
        public string ProjectSlug { get; set; } = "";
        public string ProjectShortDescription { get; set; } = "";
        public string ProjectCategory { get; set; } = "";
        public bool IsFeatured { get; set; } = false;
        public List<string> Tags { get; set; } = new List<string>();
        public string ProjectThumbnailURL { get; set; } = "/images/thumb-placeholder.jpg";
        public string ProjectThumbnailAltText { get; set; } = "No image available for this project.";
    }
}
