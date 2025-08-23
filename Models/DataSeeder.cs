using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class DataSeeder
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration, ILogger<DataSeeder> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedRolesAndAdmin()
    {
        const string adminRole = "Admin";
        const string moderatorRole = "Moderator";

        if (!await _roleManager.RoleExistsAsync(adminRole))
            await _roleManager.CreateAsync(new IdentityRole(adminRole));
        if (!await _roleManager.RoleExistsAsync(moderatorRole))
            await _roleManager.CreateAsync(new IdentityRole(moderatorRole));

        var adminEmail = _configuration["AdminEmail"] ?? "admin@example.com";
        var adminPassword = _configuration["AdminPassword"];
        var resetFlag = (_configuration["AdminResetOnStartup"] ?? "false").Equals("true", StringComparison.OrdinalIgnoreCase);

        // Fallback password if none supplied (WARNING: for initial bootstrap only)
        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            adminPassword = "admin123!"; // meets relaxed policy (uppercase disabled in Program.cs)
            _logger.LogWarning("AdminPassword not configured. Using insecure fallback password 'admin123!' – change this immediately in production.");
        }

        var user = await _userManager.FindByEmailAsync(adminEmail);
        if (user == null)
        {
            user = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true, LockoutEnabled = true };
            var createResult = await _userManager.CreateAsync(user, adminPassword);
            if (!createResult.Succeeded)
                throw new InvalidOperationException("Failed to create admin user: " + string.Join("; ", createResult.Errors.Select(e => e.Description)));

            await _userManager.AddToRoleAsync(user, adminRole);
            _logger.LogInformation("Admin user {Email} created.", adminEmail);
        }
        else
        {
            // Ensure email confirmed & role
            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user);
            }
            if (!await _userManager.IsInRoleAsync(user, adminRole))
                await _userManager.AddToRoleAsync(user, adminRole);

            // Optional reset (only if explicitly requested and password provided)
            if (resetFlag && !string.IsNullOrWhiteSpace(adminPassword))
            {
                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                await _userManager.ResetPasswordAsync(user, resetToken, adminPassword);
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow);
                await _userManager.ResetAccessFailedCountAsync(user);
                _logger.LogInformation("Admin password reset due to AdminResetOnStartup flag.");
            }
        }
    }
}
