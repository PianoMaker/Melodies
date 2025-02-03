using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Packaging;
using Microsoft.AspNetCore.Mvc;

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

        public async Task<IActionResult>OnGetAsync()
        {

            /*м'яке посилання користувача */
            var user = await _userManager.GetUserAsync(User);
            if (user is not null)
            {
                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                if (!isAdmin)
                {
                    return RedirectToPage("/Shared/AccessDenied");
                }
            }
            else return RedirectToPage("/Shared/AccessDenied");
            /**/

            // Отримуємо список користувачів  і ролей
            Users = _userManager.Users.ToList();

            Roles = _roleManager.Roles.Select(r => r.Name).ToList();

            if (Users == null || Roles == null)
            {             
                ModelState.AddModelError(string.Empty, "Data could not be loaded.");
            }

            UserRoles = new List<string>();

            // Додаємо ролі для кожного користувача 
            if (Users?.Count > 0)
            {
                foreach (var currentuser in Users)
                {
                    var roles = await _userManager.GetRolesAsync(currentuser);
                    UserRoles.AddRange(roles);
                }
            }
            return Page();
        }

        // кнопка додати роль
        /*
        [BindProperty]
        public string UserId { get; set; }
        */

        public async Task<IActionResult> OnPostAssignAdminRoleAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                // Додавання ролі адміністратора
                var result = await _userManager.AddToRoleAsync(user, "Admin");
                if (result.Succeeded)
                {
                    Console.WriteLine($"user {user.Email} is granted as admin"); 
                    return RedirectToPage();
                }
                else
                {                
                    ModelState.AddModelError(string.Empty, "Error assigning role.");
                }
            }

            return RedirectToPage();
        }

        

            public async Task<IActionResult> OnPostRemoveAdminRoleAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                // Зняття ролі адміністратора
                var result = await _userManager.RemoveFromRoleAsync(user, "Admin");
                if (result.Succeeded)
                {
                    Console.WriteLine($"user {user.Email} більше не є адміністратором");
                    return RedirectToPage();
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Error assigning role.");
                }
            }

            return RedirectToPage();
        }

        // Встановлення статусу підтвердження електронної пошти на true

        public async Task<IActionResult> OnPostConfirmEmailAsync(string userId)
        {

            Console.WriteLine("Trying to confirm user");
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"User with ID {userId} not found.");
            }
                        
            user.EmailConfirmed = true;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Email confirmed successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Error occurred while confirming the email.";
            }

            return RedirectToPage(); // Перехід назад на сторінку
        }


    }
}
