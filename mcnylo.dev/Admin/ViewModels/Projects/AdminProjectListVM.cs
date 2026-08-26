namespace mcnylo.dev.Admin.ViewModels.Projects
{
    public class AdminProjectListVM
    {
        public string Search { get; set; } = "";
        public List<AdminProjectListItemVM> Projects { get; set; } = [];
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalProjects { get; set; } = 0;
        public int TotalPages { get; set; } = 0;
    }
}
