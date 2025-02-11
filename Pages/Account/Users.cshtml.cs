using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Melodies25.Pages.Account
{
    [Authorize(Roles = "Admin")]  // Доступ лише для адміністратора
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
        public Dictionary<string, List<string>> UserRoles { get; set; }

        // Метод OnGetAsync для завантаження користувачів та їх ролей
        public async Task<IActionResult> OnGetAsync()
        {
            
                  // М1яке посилання
                  var user = await _userManager.GetUserAsync(User);
                  if (user == null || !await _userManager.IsInRoleAsync(user, "Admin"))
                  {
                      return RedirectToPage("/Shared/AccessDenied");
                  }
            
            // Завантажуємо список користувачів та ролей
            Users = _userManager.Users.ToList();
            Roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

            // Перевірка на наявність даних
            if (Users == null || Roles == null)
            {
                ModelState.AddModelError(string.Empty, "Дані не можуть бути завантажені.");
                return Page();
            }

            UserRoles = new Dictionary<string, List<string>>();

            // Завантажуємо ролі для кожного користувача
            foreach (var currentuser in Users)
            {
                var roles = await _userManager.GetRolesAsync(currentuser);
                UserRoles[currentuser.Id] = roles.ToList();
            }

            return Page();
        }

        // Додавання ролі адміністратора до користувача
        public async Task<IActionResult> OnPostAssignAdminRoleAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.AddToRoleAsync(user, "Admin");
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"{user.Email} отримав роль адміністратора.";
                return RedirectToPage();
            }

            ModelState.AddModelError(string.Empty, "Помилка при призначенні ролі.");
            return RedirectToPage();
        }

        // Видалення ролі адміністратора від користувача
        public async Task<IActionResult> OnPostRemoveAdminRoleAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            if (adminUsers.Count == 1 && adminUsers.Contains(user))
            {
                ModelState.AddModelError("", "Неможливо видалити останнього адміністратора.");
                return Page();
            }

            var result = await _userManager.RemoveFromRoleAsync(user, "Admin");
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"{user.Email} втратив роль адміністратора.";
                return RedirectToPage();
            }

            ModelState.AddModelError("", "Не вдалося видалити роль адміністратора.");
            return Page();
        }

        // Підтвердження електронної пошти
        public async Task<IActionResult> OnPostConfirmEmailAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"Користувача з ID {userId} не знайдено.");
            }

            user.EmailConfirmed = true;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Електронну пошту підтверджено успішно.";
            }
            else
            {
                TempData["ErrorMessage"] = "Сталася помилка під час підтвердження електронної пошти.";
            }

            return RedirectToPage();
        }

        // Призначення ролі модератора користувачу
        public async Task<IActionResult> OnPostAssignModeratorAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.AddToRoleAsync(user, "Moderator");
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"{user.Email} отримав роль модератора.";
            }
            else
            {
                TempData["ErrorMessage"] = "Не вдалося призначити роль модератора.";
            }

            return RedirectToPage();
        }

        // Видалення ролі модератора від користувача
        public async Task<IActionResult> OnPostRemoveModeratorAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.RemoveFromRoleAsync(user, "Moderator");
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"{user.Email} втратив роль модератора.";
            }
            else
            {
                TempData["ErrorMessage"] = "Не вдалося видалити роль модератора.";
            }

            return RedirectToPage();
        }

        // Обробник для видалення користувача
        public async Task<IActionResult> OnPostDeleteUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    // Успішне видалення
                    return RedirectToPage();
                }
            }
            return BadRequest("Не вдалося видалити користувача.");
        }
    }
}
