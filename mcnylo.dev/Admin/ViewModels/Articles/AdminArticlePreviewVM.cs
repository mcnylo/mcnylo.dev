namespace mcnylo.dev.Admin.ViewModels.Articles
{
    public class AdminArticlePreviewVM
    {
        public int Id { get; set; } = 0;
        public string ArticleTitle { get; set; } = "";
        public string ArticleSlug { get; set; } = "";
        public string ShortDescription { get; set; } = "";
        public string HtmlContent { get; set; } = "";
        public bool IsPublished { get; set; } = false;
        public DateTime? PublishedOn { get; set; }
        public string CategoryName { get; set; } = "";
        public string PrimaryImagePath { get; set; } = "";
        public string PrimaryImageAltText { get; set; } = "";
        public List<string> Tags { get; set; } = [];
    }
}
