namespace mcnylo.dev.Admin.ViewModels.Tags
{
    public class AdminTagListVM
    {
        public List<AdminTagListItemVM> Tags { get; set; } = [];
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalTags { get; set; } = 0;
        public int TotalPages { get; set; } = 0;
        public string ReturnUrl { get; set; } = "/admin";
    }
}
