namespace mcnylo.dev.Admin.ViewModels.Articles
{
    public class AdminArticleCategoryListItemVM
    {
        public int Id { get; set; } = 0;
        public string CategoryName { get; set; } = "";
        public string CategorySlug { get; set; } = "";
        public int DisplayOrder { get; set; } = 0;
        public int ArticleCount { get; set; } = 0;
    }
}
