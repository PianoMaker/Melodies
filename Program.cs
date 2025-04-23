using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Melodies25.Data;
using Microsoft.AspNetCore.Identity;
using Melodies25.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Identity.UI.Services;


namespace Melodies25
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var env = builder.Environment;

            /* Trying Azur Db*/

            var connectionString = env.IsDevelopment()
                ? builder.Configuration.GetConnectionString("SQLExpress") // Локальна БД
                : builder.Configuration.GetConnectionString("Smarter"); // Azure БД

            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("Connection string not found.");

            builder.Services.AddDbContext<Melodies25Context>(options =>
                options.UseSqlServer(connectionString));

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

            builder.Services.AddDatabaseDeveloperPageExceptionFilter();



            builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.SignIn.RequireConfirmedEmail = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
            
            builder.Services.AddControllersWithViews();

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.AccessDeniedPath = "/Account/AccessDenied"; // Перенаправлення замість 404
            });

            builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

            // Add services to the container.
            builder.Services.AddRazorPages()
                .AddDataAnnotationsLocalization(options =>
                    options.DataAnnotationLocalizerProvider = (type, factory) =>
                        factory.Create(typeof(SharedResource))
                );

            builder.Services.AddScoped<DataSeeder>();

            builder.Services.AddSingleton<IEmailSender, DummyEmailSender>();
            builder.Services.AddSession();            




            //для багатомовних ресурсів
            builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var seeder = services.GetRequiredService<DataSeeder>();
                seeder.SeedRolesAndAdmin().GetAwaiter().GetResult();
            }

            app.UseSession();

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();
            app.UseRequestLocalization(options =>
            {
                var supportedCultures = new[] { "uk", "en" };
                options.SetDefaultCulture("uk")
                       .AddSupportedCultures(supportedCultures)
                       .AddSupportedUICultures(supportedCultures);
            });
            app.MapRazorPages();

            app.Run();
        }
    }
}
