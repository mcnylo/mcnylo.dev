namespace mcnylo.dev.Admin.ViewModels.Projects
{
    public class AdminProjectCategoryListVM
    {
        public List<AdminProjectCategoryListItemVM> Categories { get; set; } = [];
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCategories { get; set; } = 0;
        public int TotalPages { get; set; } = 0;
        public string ReturnUrl { get; set; } = "/admin/projects";
    }
}
