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
        public DbSet<ArticleCategory> ArticleCategories => Set<ArticleCategory>();
        public DbSet<ArticleTag> ArticleTags => Set<ArticleTag>();
        public DbSet<AboutPage> AboutPages => Set<AboutPage>();

        // ========================================================================================

        public McNyloDbContext(DbContextOptions<McNyloDbContext> options) : base(options)
        {
        }

        // ========================================================================================

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Project>().ToTable("project");
            modelBuilder.Entity<ProjectCategory>().ToTable("projectcategory");
            modelBuilder.Entity<Tag>().ToTable("tag");
            modelBuilder.Entity<ProjectTag>().ToTable("projecttag");
            modelBuilder.Entity<ProjectMedia>().ToTable("projectmedia");
            modelBuilder.Entity<Article>().ToTable("article");
            modelBuilder.Entity<ArticleCategory>().ToTable("articlecategory");
            modelBuilder.Entity<ArticleTag>().ToTable("articletag");
            modelBuilder.Entity<AboutPage>().ToTable("aboutpage");

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

            modelBuilder.Entity<ArticleCategory>()
                .HasIndex(articleCategory => articleCategory.CategorySlug)
                .IsUnique();

            modelBuilder.Entity<ArticleCategory>()
                .Property(articleCategory => articleCategory.CategoryName)
                .HasMaxLength(100);

            modelBuilder.Entity<ArticleCategory>()
                .Property(articleCategory => articleCategory.CategorySlug)
                .HasMaxLength(100);

            modelBuilder.Entity<Article>()
                .Property(article => article.PrimaryImagePath)
                .HasMaxLength(500);

            modelBuilder.Entity<Article>()
                .Property(article => article.PrimaryImageAltText)
                .HasMaxLength(250);

            modelBuilder.Entity<Article>()
            .Property(article => article.IsPublished)
            .HasColumnType("bit(1)");

            modelBuilder.Entity<Article>()
                .HasOne(article => article.ArticleCategory)
                .WithMany(articleCategory => articleCategory.Articles)
                .HasForeignKey(article => article.ArticleCategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ArticleTag>()
                .HasKey(articleTag => new { articleTag.ArticleId, articleTag.TagId });

            modelBuilder.Entity<ArticleTag>()
                .HasOne(articleTag => articleTag.Article)
                .WithMany(article => article.ArticleTags)
                .HasForeignKey(articleTag => articleTag.ArticleId);

            modelBuilder.Entity<ArticleTag>()
                .HasOne(articleTag => articleTag.Tag)
                .WithMany()
                .HasForeignKey(articleTag => articleTag.TagId);

            modelBuilder.Entity<AboutPage>()
                .Property(aboutPage => aboutPage.DisplayName)
                .HasMaxLength(100);

            modelBuilder.Entity<AboutPage>()
                .Property(aboutPage => aboutPage.ProfileSummary)
                .HasMaxLength(500);

            modelBuilder.Entity<AboutPage>()
                .Property(aboutPage => aboutPage.ResumePdfUrl)
                .HasMaxLength(500);

            modelBuilder.Entity<AboutPage>()
                .Property(aboutPage => aboutPage.IntroductionHeading)
                .HasMaxLength(200);

            modelBuilder.Entity<AboutPage>()
                .Property(aboutPage => aboutPage.IntroductionMarkdown)
                .HasColumnType("longtext");

            modelBuilder.Entity<AboutPage>()
                .Property(aboutPage => aboutPage.ExperienceHeading)
                .HasMaxLength(200);

            modelBuilder.Entity<AboutPage>()
                .Property(aboutPage => aboutPage.ExperienceMarkdown)
                .HasColumnType("longtext");

            modelBuilder.Entity<AboutPage>()
                .Property(aboutPage => aboutPage.EducationHeading)
                .HasMaxLength(200);

            modelBuilder.Entity<AboutPage>()
                .Property(aboutPage => aboutPage.EducationMarkdown)
                .HasColumnType("longtext");

            modelBuilder.Entity<AboutPage>()
                .Property(aboutPage => aboutPage.InterestsHeading)
                .HasMaxLength(200);

            modelBuilder.Entity<AboutPage>()
                .Property(aboutPage => aboutPage.InterestsMarkdown)
                .HasColumnType("longtext");
        }
    }
}
