using Microsoft.EntityFrameworkCore;
using Melodies25.Data;
using Microsoft.AspNetCore.Identity;
using Melodies25.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Localization;

namespace Melodies25
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var env = builder.Environment;

            // Layered configuration
            builder.Configuration
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables(); // allow override via env vars

            // Connection string resolution (env var override > configured fallback)
            // Use environment variable CONNECTION_STRING if present
            var directConn = builder.Configuration["CONNECTION_STRING"]; // custom override
            string connectionString;
            if (!string.IsNullOrWhiteSpace(directConn))
            {
                connectionString = directConn;
            }
            else
            {
                // existing logic with fallbacks
                connectionString = env.IsDevelopment()
                    ? builder.Configuration.GetConnectionString("SQLExpress")
                    : builder.Configuration.GetConnectionString("Smarter");
            }

            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("Connection string not found.");

            builder.Services.AddDbContext<Melodies25Context>(options => options.UseSqlServer(connectionString));
            builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
            {
                // Stricter defaults for production; can be relaxed in Development via appsettings if desired
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.SignIn.RequireConfirmedAccount = true;
                options.SignIn.RequireConfirmedEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            bool allowHttp = builder.Configuration.GetValue("AllowHttp", true); // default true per request

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.SlidingExpiration = true;
                options.Cookie.HttpOnly = true;
                // Allow HTTP if flag enabled
                options.Cookie.SecurePolicy = allowHttp ? CookieSecurePolicy.None : CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
            });

            builder.Services.AddControllersWithViews();

            builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

            builder.Services.AddRazorPages()
                .AddDataAnnotationsLocalization(options =>
                    options.DataAnnotationLocalizerProvider = (type, factory) =>
                        factory.Create(typeof(SharedResource))
                );

            builder.Services.AddScoped<DataSeeder>();

            // Email sender: try SMTP, fallback to dummy
            if (!string.IsNullOrWhiteSpace(builder.Configuration["Smtp:Host"]) && !string.IsNullOrWhiteSpace(builder.Configuration["Smtp:User"]) && !string.IsNullOrWhiteSpace(builder.Configuration["Smtp:Password"]))
            {
                builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
            }
            else
            {
                builder.Services.AddSingleton<IEmailSender, DummyEmailSender>();
            }

            builder.Services.AddSession();

            builder.Services.AddResponseCompression();

            var app = builder.Build();

            // Apply migrations automatically in Production (optional in Dev)
            using (var scope = app.Services.CreateScope())
            {
                try
                {
                    var services = scope.ServiceProvider;
                    var melodiesCtx = services.GetRequiredService<Melodies25Context>();
                    var appCtx = services.GetRequiredService<ApplicationDbContext>();
                    melodiesCtx.Database.Migrate();
                    appCtx.Database.Migrate();

                    var seeder = services.GetRequiredService<DataSeeder>();
                    // Ensure roles/admin present
                    seeder.SeedRolesAndAdmin().GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    app.Logger.LogError(ex, "Database migration / seeding failed");
                    throw; // fail fast
                }
            }

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // HSTS intentionally disabled to allow plain HTTP
                // app.UseHsts();
            }
            else
            {
                app.UseDeveloperExceptionPage();
            }

            // Keep HTTPS redirection disabled to allow HTTP access
            // app.UseHttpsRedirection();

            // Static files with basic caching headers
            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = ctx =>
                {
                    const int days = 30;
                    ctx.Context.Response.Headers["Cache-Control"] = $"public,max-age={days * 86400}";
                }
            });

            app.UseResponseCompression();
            app.UseSession();

            app.UseRouting();

            // Security headers (basic set; CSP omitted until inline scripts reviewed)
            /*
            app.Use(async (context, next) =>
            {
                var headers = context.Response.Headers;
                if (!headers.ContainsKey("X-Content-Type-Options")) headers.Add("X-Content-Type-Options", "nosniff");
                if (!headers.ContainsKey("X-Frame-Options")) headers.Add("X-Frame-Options", "DENY");
                if (!headers.ContainsKey("Referrer-Policy")) headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
                if (!headers.ContainsKey("X-XSS-Protection")) headers.Add("X-XSS-Protection", "0");
                await next();
            });
            */

            // Localization (before auth if UI culture affects auth flows)
            var supportedCultures = new[] { "uk", "en" };
            app.UseRequestLocalization(options =>
            {
                options.SetDefaultCulture("uk")
                       .AddSupportedCultures(supportedCultures)
                       .AddSupportedUICultures(supportedCultures);
            });

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapRazorPages();

            // Initialize logging manager (loads loggingsettings.json if present)
            Utilities.LoggingManager.Initialize(app.Environment);

            app.Run();
        }
    }
}
