namespace mcnylo.dev.Data.Models
{
    public class ArticleTag
    {
        public int ArticleId { get; set; } = 0;
        public Article? Article { get; set; } = null;

        public int TagId { get; set; } = 0;
        public Tag? Tag { get; set; } = null;
    }
}
