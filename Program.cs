﻿using Microsoft.EntityFrameworkCore;
using Melodies25.Data;
using Microsoft.AspNetCore.Identity;
using Melodies25.Models;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Melodies25
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // SOURCE context: VisualStudio (LocalDB) – only used for reading / syncing FROM
            builder.Services.AddDbContext<Melodies25SourceContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("VisualStudio")));

            // TARGET context: SQLExpress – destination for synchronization
            builder.Services.AddDbContext<Melodies25TargetContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("SQLExpress")));

            // WORKING context (used by application pages): also SQLExpress now
            builder.Services.AddDbContext<Melodies25Context>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("SQLExpress")));

            // Identity context (can be unified later if desired)
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.SignIn.RequireConfirmedEmail = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            builder.Services.AddSingleton<IEmailSender, Services.DummyEmailSender>();

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.AccessDeniedPath = "/Account/AccessDenied";
            });

            builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

            builder.Services.AddRazorPages()
                .AddDataAnnotationsLocalization(options =>
                    options.DataAnnotationLocalizerProvider = (type, factory) =>
                        factory.Create(typeof(SharedResource))
                );

            builder.Services.AddScoped<DatabaseSyncService>();

            builder.Services.AddSession();
            builder.Services.AddLogging();

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseSession();
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapRazorPages();

            // Run one-way sync (source -> target) at startup
            using (var scope = app.Services.CreateScope())
            {
                var syncService = scope.ServiceProvider.GetRequiredService<DatabaseSyncService>();
                syncService.SyncDatabasesAsync().GetAwaiter().GetResult();
            }

            app.Run();
        }
    }
}
