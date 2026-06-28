namespace mcnylo.dev.Projects.ViewModels
{
    public class ProjectFilterVM
    {
        public string? Search { get; set; }
        public List<string> CategorySlugs { get; set; } = [];
        public List<string> TagSlugs { get; set; } = [];
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 5;
    }
}
