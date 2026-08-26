using System.ComponentModel.DataAnnotations;

namespace mcnylo.dev.Admin.ViewModels.Projects
{
    public class AdminProjectFormVM
    {
        public int Id { get; set; } = 0;

        [Required, StringLength(200)]
        public string ProjectTitle { get; set; } = "";

        [Required, StringLength(200)]
        [RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$", ErrorMessage = "Use lowercase letters, numbers, and hyphens only.")]
        public string ProjectSlug { get; set; } = "";

        [Required, StringLength(500)]
        public string ShortDescription { get; set; } = "";

        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string LongDescription { get; set; } = "";

        [Range(1, int.MaxValue, ErrorMessage = "Select a category.")]
        public int CategoryId { get; set; } = 0;

        [StringLength(500)]
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string RepositoryURL { get; set; } = "";
        public bool IsFeatured { get; set; } = false;
        public List<int> SelectedTagIds { get; set; } = [];
        public int PrimaryMediaIndex { get; set; } = 0;
        public List<AdminProjectCategoryOptionVM> Categories { get; set; } = [];
        public List<AdminProjectTagOptionVM> AvailableTags { get; set; } = [];
        public List<AdminProjectMediaFormVM> MediaItems { get; set; } = [];
    }
}
