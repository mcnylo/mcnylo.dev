namespace mcnylo.dev.Data.Models
{
    public class ProjectMedia
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public Project Project { get; set; } = new Project();
        public string MediaType { get; set; } = "";
        public string MediaURL { get; set; } = "";
        public string? ThumbnailURL { get; set; }
        public string? AltText { get; set; }
        public int SortOrder { get; set; }
        public bool IsPrimary { get; set; }
    }
}
