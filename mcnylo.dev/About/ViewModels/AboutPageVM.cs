namespace mcnylo.dev.About.ViewModels
{
    public class AboutPageVM
    {
        public string DisplayName { get; set; } = "";
        public string ProfileSummary { get; set; } = "";
        public List<AboutSectionVM> Sections { get; set; } = [];
        public string? ResumePdfUrl { get; set; } = null;
    }
}
