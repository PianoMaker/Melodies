using Microsoft.AspNetCore.Identity;

public class DataSeeder
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public DataSeeder(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task SeedRolesAndAdmin()
    {
        string adminRole = "Admin";
        string moderatorRole = "Moderator";

        if (!await _roleManager.RoleExistsAsync(adminRole))
        {
            await _roleManager.CreateAsync(new IdentityRole(adminRole));
        }

        if (!await _roleManager.RoleExistsAsync(moderatorRole))
        {
            await _roleManager.CreateAsync(new IdentityRole(moderatorRole));
        }



        string adminEmail = "admin@example.com";
        string adminPassword = "Admin123!";

        var user = await _userManager.FindByEmailAsync(adminEmail);
        if (user == null)
        {
            user = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
            await _userManager.CreateAsync(user, adminPassword);
            await _userManager.AddToRoleAsync(user, adminRole);
        }
    }
}
