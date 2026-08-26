namespace mcnylo.dev.Admin.ViewModels.Projects
{
    public class AdminProjectCategoryListItemVM
    {
        public int Id { get; set; } = 0;
        public string CategoryName { get; set; } = "";
        public string CategorySlug { get; set; } = "";
        public int ProjectCount { get; set; } = 0;
    }
}
