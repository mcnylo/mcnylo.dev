namespace mcnylo.dev.Data.Models
{
    public class AboutPage
    {
        public int Id { get; set; } = 0;
        public string DisplayName { get; set; } = "";
        public string ProfileSummary { get; set; } = "";
        public string IntroductionHeading { get; set; } = "";
        public string IntroductionMarkdown { get; set; } = "";
        public string ExperienceHeading { get; set; } = "";
        public string ExperienceMarkdown { get; set; } = "";
        public string EducationHeading { get; set; } = "";
        public string EducationMarkdown { get; set; } = "";
        public string InterestsHeading { get; set; } = "";
        public string InterestsMarkdown { get; set; } = "";
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedOn { get; set; } = null;
    }
}
