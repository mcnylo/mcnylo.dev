namespace mcnylo.dev.Admin.ViewModels.Articles
{
    public class AdminArticleListVM
    {
        public string Search { get; set; } = "";
        public string Status { get; set; } = "all";
        public List<AdminArticleListItemVM> Articles { get; set; } = [];
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalArticles { get; set; } = 0;
        public int TotalPages { get; set; } = 0;
    }
}
