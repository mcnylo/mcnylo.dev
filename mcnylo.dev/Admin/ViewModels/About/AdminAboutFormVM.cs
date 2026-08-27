using System.ComponentModel.DataAnnotations;

namespace mcnylo.dev.Admin.ViewModels.About
{
    public class AdminAboutFormVM
    {
        public int Id { get; set; } = 0;

        [Required, StringLength(100)]
        public string DisplayName { get; set; } = "";

        [Required, StringLength(500)]
        public string ProfileSummary { get; set; } = "";

        [StringLength(500)]
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string ResumePdfUrl { get; set; } = "";
        public IFormFile? ResumePdfFile { get; set; } = null;

        [Required, StringLength(200)]
        public string IntroductionHeading { get; set; } = "";

        [Required]
        public string IntroductionMarkdown { get; set; } = "";

        [Required, StringLength(200)]
        public string ExperienceHeading { get; set; } = "";

        [Required]
        public string ExperienceMarkdown { get; set; } = "";

        [Required, StringLength(200)]
        public string EducationHeading { get; set; } = "";

        [Required]
        public string EducationMarkdown { get; set; } = "";

        [Required, StringLength(200)]
        public string InterestsHeading { get; set; } = "";

        [Required]
        public string InterestsMarkdown { get; set; } = "";
    }
}
