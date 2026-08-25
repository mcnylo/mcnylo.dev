namespace mcnylo.dev.Admin.ViewModels.Tags
{
    public class AdminTagDeleteVM
    {
        public int Id { get; set; } = 0;
        public string TagName { get; set; } = "";
        public string TagSlug { get; set; } = "";
        public int ArticleCount { get; set; } = 0;
        public int ProjectCount { get; set; } = 0;
        public string ReturnUrl { get; set; } = "/admin/tags";
    }
}
