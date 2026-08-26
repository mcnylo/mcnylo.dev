namespace mcnylo.dev.Admin.ViewModels.Projects
{
    public class AdminProjectCategoryDeleteVM
    {
        public int Id { get; set; } = 0;
        public string CategoryName { get; set; } = "";
        public string CategorySlug { get; set; } = "";
        public int ProjectCount { get; set; } = 0;
        public string ReturnUrl { get; set; } = "/admin/project-categories";
        public bool CanDelete => ProjectCount == 0;
    }
}
