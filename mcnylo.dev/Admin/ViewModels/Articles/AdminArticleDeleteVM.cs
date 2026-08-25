namespace mcnylo.dev.Admin.ViewModels.Articles
{
    public class AdminArticleDeleteVM
    {
        public int Id { get; set; } = 0;
        public string ArticleTitle { get; set; } = "";
        public string ArticleSlug { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public bool IsPublished { get; set; } = false;
        public DateTime CreatedOn { get; set; }
        public DateTime? PublishedOn { get; set; }
    }
}
