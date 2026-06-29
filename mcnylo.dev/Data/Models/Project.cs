namespace mcnylo.dev.Data.Models
{
    public class Project
    {
        public int Id { get; set; } = 0;
        public string ProjectTitle { get; set; } = "";
        public string ProjectSlug { get; set; } = "";
        public string ShortDescription { get; set; } = "";
        public string? LongDescription { get; set; } = "";
        public int CategoryId { get; set; } = 0;
        public ProjectCategory Category { get; set; } = new ProjectCategory();
        public string? RepositoryURL { get; set; } = "";
        public bool IsFeatured { get; set; } = false;
        public DateTime CreatedOn { get; set; } = DateTime.Now;
        public DateTime? UpdatedOn {  get; set; } = null;
        public ICollection<ProjectTag> ProjectTags { get; set; } = [];
        public List<ProjectMedia> MediaItems { get; set; } = new List<ProjectMedia>();
    }
}
