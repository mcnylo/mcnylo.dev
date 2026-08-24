namespace mcnylo.dev.Articles.ViewModels
{
    public class ArticleListItemVM
    {
        public string ArticleTitle { get; set; } = "";
        public string ArticleSlug { get; set; } = "";
        public string ShortDescription { get; set; } = "";
        public DateTime? PublishedOn { get; set; } = null;
        public string CategoryName { get; set; } = "";
        public string CategorySlug { get; set; } = "";
        public string PrimaryImagePath { get; set; } = "";
        public string PrimaryImageAltText { get; set; } = "";
        public List<ArticleTagVM> Tags { get; set; } = [];
    }
}
