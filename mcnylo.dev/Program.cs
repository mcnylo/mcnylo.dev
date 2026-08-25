using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using mcnylo.dev.Data.Context;
using mcnylo.dev.Home.Services;
using mcnylo.dev.Projects.Services;
using mcnylo.dev.About.Services;
using mcnylo.dev.Articles.Services;

namespace mcnylo.dev
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews()
                .AddRazorOptions(options =>
                {
                    options.ViewLocationFormats.Clear();

                    options.ViewLocationFormats.Add("/{1}/Views/{0}.cshtml");
                    options.ViewLocationFormats.Add("/Views/Shared/{0}.cshtml");
                });

            var connectionString = builder.Configuration.GetConnectionString("HomeConnection");

            builder.Services.AddDbContext<McNyloDbContext>(options => options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
            builder.Services.AddScoped<IHomeService, HomeService>();
            builder.Services.AddScoped<IProjectService, ProjectService>();
            builder.Services.AddScoped<IAboutService, AboutService>();
            builder.Services.AddScoped<IArticleMarkdownService, ArticleMarkdownService>();
            builder.Services.AddScoped<IArticleService, ArticleService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            string projectMediaRootPath = builder.Configuration["MediaStorage:ProjectMediaRootPath"] ?? throw new InvalidOperationException("Project media root path is not configured.");
            string projectMediaRequestPath = builder.Configuration["MediaStorage:ProjectMediaRequestPath"] ?? throw new InvalidOperationException("Project media request path is not configured.");

            Directory.CreateDirectory(projectMediaRootPath);

            app.UseHttpsRedirection();

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(projectMediaRootPath),
                RequestPath = projectMediaRequestPath
            });

            app.UseRouting();
            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
