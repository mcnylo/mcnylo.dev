namespace mcnylo.dev.Admin.ViewModels.Projects
{
    public class AdminProjectMediaFormVM
    {
        public string? MediaType { get; set; } = "IMAGE";
        public IFormFile? ImageFile { get; set; }
        public string? YouTubeUrl { get; set; } = "";
        public string? AltText { get; set; } = "";
        public int? SortOrder { get; set; } = 0;
        public int PrimaryMediaIndex { get; set; } = 0;
    }
}
