using Melodies25.Data;
using Melodies25.Models;
using Melodies25.Services; // Додано для використання сервісів очистки
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging; // Для логування
using Microsoft.Extensions.Options;
using System.Text;

namespace Melodies25
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // Prefer UTF-8 console encoding so Ukrainian characters print/read correctly
            try
            {
                Console.OutputEncoding = Encoding.UTF8;
                Console.InputEncoding = Encoding.UTF8;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: cannot set console encoding to UTF-8: {ex.Message}");
            }

            var builder = WebApplication.CreateBuilder(args);

            // 1. Додаємо сервіси локалізації та вказуємо шлях до ресурсів.
            builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

            // 2. Додаємо Razor Pages та підтримку локалізації для них.
            builder.Services.AddRazorPages()
                .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
                .AddDataAnnotationsLocalization();

           
            // Створюємо логер для відлагодження
            var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();

            // Читання налаштувань з конфігураційного файлу
            builder.Configuration
                .SetBasePath(builder.Environment.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            // Отримуємо рядок підключення до бази даних          
            string connectionString = builder.Configuration.GetConnectionString("VisualStudio");
            logger.LogInformation($"connectionString: {connectionString}");

            // Перевірка на порожній рядок підключення
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("Connection string is empty or invalid.");

            // Реєстрація контекстів для доступу до бази даних
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

            // Додавання інших сервісів
            builder.Services.AddRazorPages();

            builder.Services.AddSingleton<IEmailSender, DummyEmailSender>();

            // Налаштування сервісу для очищення тимчасових файлів
            builder.Services.Configure<Melodies25.Services.TemporaryFilesCleanupOptions>(
                builder.Configuration.GetSection("TemporaryFilesCleanup"));
            builder.Services.AddHostedService<Melodies25.Services.TemporaryFilesCleanupService>();

            var app = builder.Build();

            // 3. Налаштовуємо підтримувані культури.
            var supportedCultures = new[] { "uk", "en" };
            var localizationOptions = new RequestLocalizationOptions().SetDefaultCulture(supportedCultures[0])
                .AddSupportedCultures(supportedCultures)
                .AddSupportedUICultures(supportedCultures);

            app.UseRequestLocalization(localizationOptions);

            // Налаштування обробки помилок
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage(); // Для більш детальної інформації про помилки у Development
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            //щоб завантажувалось все що в css, js та інших локаціях
            app.UseStaticFiles();

            // Налаштування маршрутизації
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapRazorPages();
            
            // Initialize logging manager (loads loggingsettings.json if present)
            Utilities.LoggingManager.Initialize(app.Environment);

            app.Run();
        }
    }
}
// версії AspNetCore.Mvc.Razor
// локально - 8.0.1425.11221
// на asp.net - 8.0.1224.60312