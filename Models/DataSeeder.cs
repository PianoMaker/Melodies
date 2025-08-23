using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

public class DataSeeder
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;

    public DataSeeder(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
    }

    public async Task SeedRolesAndAdmin()
    {
        const string adminRole = "Admin";
        const string moderatorRole = "Moderator";

        if (!await _roleManager.RoleExistsAsync(adminRole))
            await _roleManager.CreateAsync(new IdentityRole(adminRole));
        if (!await _roleManager.RoleExistsAsync(moderatorRole))
            await _roleManager.CreateAsync(new IdentityRole(moderatorRole));
        // Credentials from configuration (user-secrets / env vars)
        var adminEmail = _configuration["AdminEmail"] ?? "admin@example.com"; // fallback only for dev
        var adminPassword = _configuration["AdminPassword"]; // must be supplied via secrets/env for first creation
        var resetFlag = (_configuration["AdminResetOnStartup"] ?? "false").Equals("true", StringComparison.OrdinalIgnoreCase);

        var user = await _userManager.FindByEmailAsync(adminEmail);
        if (user == null)
        {
            if (string.IsNullOrWhiteSpace(adminPassword))
                throw new InvalidOperationException("AdminPassword is not configured. Set user secret AdminPassword before first run.");

            user = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true, LockoutEnabled = true };
            var createResult = await _userManager.CreateAsync(user, adminPassword);
            if (!createResult.Succeeded)
                throw new InvalidOperationException("Failed to create admin user: " + string.Join("; ", createResult.Errors.Select(e => e.Description)));

            await _userManager.AddToRoleAsync(user, adminRole);
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
            }
        }
    }
}
