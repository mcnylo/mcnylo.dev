using System.ComponentModel.DataAnnotations;

namespace mcnylo.dev.Admin.ViewModels.Articles
{
    public class AdminArticleFormVM
    {
        public int Id { get; set; } = 0;

        [Required, StringLength(200)]
        public string ArticleTitle { get; set; } = "";

        [Required, StringLength(200)]
        [RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$", ErrorMessage = "Use lowercase letters, numbers, and hyphens only.")]
        public string ArticleSlug { get; set; } = "";

        [Required, StringLength(500)]
        public string ShortDescription { get; set; } = "";

        [Required]
        public string MarkdownContent { get; set; } = "";

        public int? ArticleCategoryId { get; set; }

        [StringLength(500)]
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string PrimaryImagePath { get; set; } = "";

        [StringLength(250)]
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string PrimaryImageAltText { get; set; } = "";
        public IFormFile? PrimaryImageFile { get; set; }

        public bool IsPublished { get; set; } = false;
        public List<int> SelectedTagIds { get; set; } = [];

        public List<AdminArticleCategoryOptionVM> Categories { get; set; } = [];
        public List<AdminArticleTagOptionVM> AvailableTags { get; set; } = [];
    }
}
