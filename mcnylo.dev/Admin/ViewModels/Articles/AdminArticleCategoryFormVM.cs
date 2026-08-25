using System.ComponentModel.DataAnnotations;

namespace mcnylo.dev.Admin.ViewModels.Articles
{
    public class AdminArticleCategoryFormVM
    {
        public int Id { get; set; } = 0;

        [Required, StringLength(100)]
        public string CategoryName { get; set; } = "";

        [Required, StringLength(100)]
        [RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$", ErrorMessage = "Use lowercase letters, numbers, and hyphens only.")]
        public string CategorySlug { get; set; } = "";

        public int DisplayOrder { get; set; } = 0;
    }
}
