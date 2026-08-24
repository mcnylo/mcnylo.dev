namespace mcnylo.dev.Data.Models
{
    public class ArticleCategory
    {
        public int Id { get; set; } = 0;
        public string CategoryName { get; set; } = "";
        public string CategorySlug { get; set; } = "";
        public int DisplayOrder { get; set; } = 0;

        public ICollection<Article> Articles { get; set; } = [];
    }
}
