namespace mcnylo.dev.Admin.ViewModels.Media
{
    public class AdminMediaListItemVM
    {
        public string FileName { get; set; } = "";
        public string RequestPath { get; set; } = "";
        public string RelativePath { get; set; } = "";
        public string MediaArea { get; set; } = "";
        public long FileSizeInBytes { get; set; } = 0;
        public DateTime LastModifiedOn { get; set; } = DateTime.MinValue;
        public int ArticleReferenceCount { get; set; } = 0;
        public int ProjectReferenceCount { get; set; } = 0;
        public bool IsReferenced => ArticleReferenceCount > 0 || ProjectReferenceCount > 0;
    }
}
