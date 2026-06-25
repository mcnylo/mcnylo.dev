namespace mcnylo.dev.Data.Models
{
    public class ProjectCategory
    {
        public int Id { get; set; } = 0;
        public string CategoryName { get; set; } = "";
        public string CategorySlug { get; set; } = "";
        public ICollection<Project> Projects { get; set; } = [];
    }
}
