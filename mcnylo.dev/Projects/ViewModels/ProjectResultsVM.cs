namespace mcnylo.dev.Projects.ViewModels
{
    public class ProjectResultsVM
    {
        public List<ProjectCardVM> Projects { get; set; } = new List<ProjectCardVM>();
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalProjects { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}
