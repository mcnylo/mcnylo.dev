namespace mcnylo.dev.Articles.ViewModels
{
    public class ArticlePagedResultVM
    {
        public List<ArticleListItemVM> Articles { get; set; } = [];
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 6;
        public int TotalArticles { get; set; } = 0;
        public int TotalPages { get; set; } = 0;
    }
}
