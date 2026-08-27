namespace mcnylo.dev.Admin.ViewModels.Projects
{
    public class AdminProjectMediaPreviewVM
    {
        public string MediaType { get; set; } = "";
        public string MediaURL { get; set; } = "";
        public string? ThumbnailURL { get; set; }
        public string AltText { get; set; } = "";
        public int SortOrder { get; set; }
        public bool IsPrimary { get; set; }
        public bool IsImage => MediaType.Equals("IMAGE");
        public bool IsVideo => MediaType.Equals("VIDEO");
    }
}
