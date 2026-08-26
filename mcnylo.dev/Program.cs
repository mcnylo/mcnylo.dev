using mcnylo.dev.About.Services;
using mcnylo.dev.Articles.Services;
using mcnylo.dev.Data.Context;
using mcnylo.dev.Home.Services;
using mcnylo.dev.Media.Services;
using mcnylo.dev.Media.Services.Articles;
using mcnylo.dev.Projects.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System.Threading.RateLimiting;

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
            builder.Services.AddSingleton<IMediaStorageService, MediaStorageService>();
            builder.Services.AddScoped<IArticleImageUploadService, ArticleImageUploadService>();

            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });

            var dataProtectionKeyPath = builder.Configuration["DataProtection:KeyPath"];

            //if (builder.Environment.IsProduction() && string.IsNullOrWhiteSpace(dataProtectionKeyPath))
            //{
            //    throw new InvalidOperationException("DataProtection:KeyPath must be configured in production.");
            //}

            var dataProtectionBuilder = builder.Services.AddDataProtection().SetApplicationName("mcnylo.dev");

            if (!string.IsNullOrWhiteSpace(dataProtectionKeyPath))
            {
                Directory.CreateDirectory(dataProtectionKeyPath);
                dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeyPath));
            }

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
            {
                options.LoginPath = "/admin/login";
                options.LogoutPath = "/admin/logout";
                options.AccessDeniedPath = "/admin/login";

                options.Cookie.Name = "McNylo.Admin";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromHours(4);
            });

            builder.Services.AddAuthorization();

            builder.Services.AddRateLimiter(options =>
            {
                options.AddPolicy("AdminLogin", httpContext =>
                
                    RateLimitPartition.GetFixedWindowLimiter(httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(5),
                        QueueLimit = 0
                    })
                );
            });

            builder.WebHost.ConfigureKestrel(options =>
            {
                options.AddServerHeader = false;
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            var mediaStorageService = app.Services.GetRequiredService<IMediaStorageService>();

            app.UseForwardedHeaders();
            app.UseHttpsRedirection();

            app.Use(async (context, next) =>
            {
                context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
                context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
                context.Response.Headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
                context.Response.Headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
                context.Response.Headers.TryAdd("Cross-Origin-Opener-Policy", "same-origin");
                context.Response.Headers.TryAdd("Cross-Origin-Resource-Policy", "same-origin");
                context.Response.Headers.TryAdd("X-Permitted-Cross-Domain-Policies", "none");
                context.Response.Headers.TryAdd(
                    "Content-Security-Policy",
                    "default-src 'self'; " +
                    "base-uri 'self'; " +
                    "object-src 'none'; " +
                    "frame-ancestors 'none'; " +
                    "form-action 'self'; " +
                    "img-src 'self' data: blob:; " +
                    "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://cdnjs.cloudflare.com; " +
                    "font-src 'self' data: https://fonts.gstatic.com https://cdnjs.cloudflare.com; " +
                    "script-src 'self' 'unsafe-inline'; " +
                    "connect-src 'self'; " +
                    "upgrade-insecure-requests");


                await next();
            });

            app.UseWhen(context => context.Request.Path.StartsWithSegments("/admin"), adminApp =>
            {
                adminApp.Use(async (context, next) =>
                {
                    context.Response.Headers.CacheControl = "no-store, no-cache, max-age=0";
                    context.Response.Headers.Pragma = "no-cache";
                    context.Response.Headers.Expires = "0";

                    await next();
                });
            });

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(mediaStorageService.RootPath),
                RequestPath = mediaStorageService.RequestPath,
                ServeUnknownFileTypes = false
            });

            app.UseRouting();
            app.UseRateLimiter();
            app.UseAuthentication();
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
