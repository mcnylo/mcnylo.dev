namespace mcnylo.dev.Data.Models
{
    public class Tag
    {
        public int Id { get; set; } = 0;
        public string TagName { get; set; } = "";
        public string TagSlug { get; set; } = "";
        public ICollection<ProjectTag> ProjectTags { get; set; } = [];
    }
}
