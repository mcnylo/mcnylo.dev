namespace mcnylo.dev.Data.Models
{
    public class Article
    {
        public int Id { get; set; } = 0;
        public string ArticleTitle { get; set; } = "";
        public string ArticleSlug { get; set; } = "";
        public string ShortDescription { get; set; } = "";
        public string MarkdownContent { get; set; } = "";
        public bool IsPublished { get; set; } = false;
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedOn { get; set; } = null;
        public DateTime? PublishedOn { get; set; } = null;
        public int? ArticleCategoryId { get; set; } = null;
        public ArticleCategory? ArticleCategory { get; set; } = null;
        public string PrimaryImagePath { get; set; } = "";
        public string PrimaryImageAltText { get; set; } = "";
        public ICollection<ArticleTag> ArticleTags { get; set; } = [];
    }
}
