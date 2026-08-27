namespace mcnylo.dev.About.ViewModels
{
    public class AboutSectionVM
    {
        public string AnchorId { get; set; } = "";
        public string NavigationLabel { get; set; } = "";
        public string NavigationIndex { get; set; } = "";
        public string Heading { get; set; } = "";
        public string HtmlContent { get; set; } = "";
        public bool HasDividerAfter { get; set; } = true;
    }
}
