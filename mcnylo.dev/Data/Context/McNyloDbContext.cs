using mcnylo.dev.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace mcnylo.dev.Data.Context
{
    public class McNyloDbContext : DbContext
    {
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<ProjectCategory> ProjectCategories => Set<ProjectCategory>();
        public DbSet<Tag> Tags => Set<Tag>();
        public DbSet<ProjectTag> ProjectTags => Set<ProjectTag>();
        public DbSet<ProjectMedia> ProjectMedia => Set<ProjectMedia>();
        public DbSet<Article> Articles => Set<Article>();

        public McNyloDbContext(DbContextOptions<McNyloDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Project>().ToTable("project");
            modelBuilder.Entity<ProjectCategory>().ToTable("projectcategory");
            modelBuilder.Entity<Tag>().ToTable("tag");
            modelBuilder.Entity<ProjectTag>().ToTable("projecttag");
            modelBuilder.Entity<ProjectMedia>().ToTable("projectmedia");
            modelBuilder.Entity<Article>().ToTable("article");

            modelBuilder.Entity<ProjectTag>().HasKey(x => new
            {
                x.ProjectId,
                x.TagId
            });

            modelBuilder.Entity<Project>()
                .HasOne(proj => proj.Category)
                .WithMany(cat => cat.Projects)
                .HasForeignKey(proj => proj.CategoryId);

            modelBuilder.Entity<ProjectTag>()
                .HasOne(protag => protag.Project)
                .WithMany(proj => proj.ProjectTags)
                .HasForeignKey(protag => protag.ProjectId);

            modelBuilder.Entity<ProjectTag>()
                .HasOne(protag => protag.Tag)
                .WithMany(tag => tag.ProjectTags)
                .HasForeignKey(protag => protag.TagId);

            modelBuilder.Entity<ProjectMedia>()
                .HasOne(media => media.Project)
                .WithMany(project => project.MediaItems)
                .HasForeignKey(media => media.ProjectId);

            modelBuilder.Entity<Article>()
                .HasIndex(article => article.ArticleSlug)
                .IsUnique();

            modelBuilder.Entity<Article>()
                .Property(article => article.ArticleTitle)
                .HasMaxLength(200);

            modelBuilder.Entity<Article>()
                .Property(article => article.ArticleSlug)
                .HasMaxLength(200);

            modelBuilder.Entity<Article>()
                .Property(article => article.ShortDescription)
                .HasMaxLength(500);

            modelBuilder.Entity<Article>()
                .Property(article => article.MarkdownContent)
                .HasColumnType("longtext");
        }
    }
}
