namespace mcnylo.dev.Admin.ViewModels.Projects
{
    public class AdminProjectPreviewVM
    {
        public int Id { get; set; } = 0;
        public string ProjectTitle { get; set; } = "";
        public string ProjectSlug { get; set; } = "";
        public string ProjectDescription { get; set; } = "";
        public string ProjectCategory { get; set; } = "";
        public string? RepositoryURL { get; set; }
        public bool IsFeatured { get; set; }
        public List<string> Tags { get; set; } = [];
        public List<AdminProjectMediaPreviewVM> MediaItems { get; set; } = [];
    }
}
