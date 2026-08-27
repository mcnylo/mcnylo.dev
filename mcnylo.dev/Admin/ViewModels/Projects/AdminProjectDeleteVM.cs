namespace mcnylo.dev.Admin.ViewModels.Projects
{
    public class AdminProjectDeleteVM
    {
        public int Id { get; set; } = 0;
        public string ProjectTitle { get; set; } = "";
        public string ProjectSlug { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public bool IsFeatured { get; set; } = false;
        public DateTime CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int TagCount { get; set; } = 0;
        public int MediaCount { get; set; } = 0;
    }
}
