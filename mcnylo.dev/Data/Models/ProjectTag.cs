namespace mcnylo.dev.Data.Models
{
    public class ProjectTag
    {
        public int ProjectId { get; set; } = 0;
        public Project Project { get; set; } = new Project();
        public int TagId { get; set; } = 0;
        public Tag Tag { get; set; } = new Tag();
    }
}
