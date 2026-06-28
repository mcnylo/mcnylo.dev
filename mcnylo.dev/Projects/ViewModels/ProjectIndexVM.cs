namespace mcnylo.dev.Projects.ViewModels
{
    public class ProjectIndexVM
    {
        public ProjectFilterVM Filter { get; set; } = new ProjectFilterVM();
        public List<FilterOptionVM> Categories { get; set; } = new List<FilterOptionVM>();
        public List<FilterOptionVM> Tags { get; set; } = new List<FilterOptionVM>();
        public ProjectResultsVM Projects { get; set; } = new();
    }
}
