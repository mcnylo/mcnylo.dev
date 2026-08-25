using System.ComponentModel.DataAnnotations;

namespace mcnylo.dev.Admin.ViewModels.Tags
{
    public class AdminTagFormVM
    {
        public int Id { get; set; } = 0;

        [Required, StringLength(100)]
        public string TagName { get; set; } = "";

        [Required, StringLength(100)]
        [RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$", ErrorMessage = "Use lowercase letters, numbers, and hyphens only.")]
        public string TagSlug { get; set; } = "";
        public string ReturnUrl { get; set; } = "/admin/tags";
    }
}
