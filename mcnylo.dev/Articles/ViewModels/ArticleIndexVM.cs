namespace mcnylo.dev.Articles.ViewModels
{
    public class ArticleIndexVM
    {
        public string Search { get; set; } = "";
        public List<string> SelectedCategorySlugs { get; set; } = [];
        public List<ArticleCategoryFilterVM> Categories { get; set; } = [];
        public List<string> SelectedTagSlugs { get; set; } = [];
        public List<ArticleTagFilterVM> AvailableTags { get; set; } = [];
        public ArticlePagedResultVM Results { get; set; } = new();
    }
}
