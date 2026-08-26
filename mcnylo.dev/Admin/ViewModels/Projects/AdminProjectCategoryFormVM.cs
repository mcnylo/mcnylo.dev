using System.ComponentModel.DataAnnotations;

namespace mcnylo.dev.Admin.ViewModels.Projects
{
    public class AdminProjectCategoryFormVM
    {
        public int Id { get; set; } = 0;

        [Required, StringLength(100)]
        public string CategoryName { get; set; } = "";

        [Required, StringLength(100)]
        [RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$", ErrorMessage = "Use lowercase letters, numbers, and hyphens only.")]
        public string CategorySlug { get; set; } = "";

        public string ReturnUrl { get; set; } = "/admin/project-categories";
    }
}
