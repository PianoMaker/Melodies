using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Packaging;

namespace Melodies25.Pages.Account
{
 //   [Authorize(Roles = "Admin")]  // Тільки для адміністратора
    public class UsersModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersModel(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public IList<IdentityUser> Users { get; set; }
        public IList<string> Roles { get; set; }
        public IList<string> UserRoles { get; set; }

        public async Task OnGetAsync()
        {
            // Отримуємо список всіх користувачів
            Users = _userManager.Users.ToList();

            // Отримуємо всі доступні ролі
            Roles = _roleManager.Roles.Select(r => r.Name).ToList();

            UserRoles = new List<string>();

            // Додаємо ролі для кожного користувача (опційно)
            foreach (var user in Users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                UserRoles.AddRange(roles);
            }
        }
    }
}
