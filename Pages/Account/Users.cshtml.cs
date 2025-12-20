using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Cryptography;

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

        // Utility: generate temp password that matches policy
        private static string GenerateTempPassword()
        {
            const string lowers = "abcdefghijklmnopqrstuvwxyz";
            const string uppers = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string digits = "0123456789";
            const string symbols = "!@#$%^&*?_";
            string all = lowers + uppers + digits + symbols;
            var rnd = RandomNumberGenerator.Create();
            string Pick(string src)
            {
                var bytes = new byte[4];
                rnd.GetBytes(bytes);
                var idx = (int)(BitConverter.ToUInt32(bytes, 0) % (uint)src.Length);
                return src[idx].ToString();
            }
            var chars = new List<char>
            {
                Pick(lowers)[0], Pick(uppers)[0], Pick(digits)[0], Pick(symbols)[0]
            };
            while (chars.Count < 12) chars.Add(Pick(all)[0]);
            chars = chars.OrderBy(_ => Guid.NewGuid()).ToList();
            return new string(chars.ToArray());
        }

        // Скидання пароля
        public async Task<IActionResult> OnPostResetPasswordAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return NotFound();
            }

            var target = await _userManager.FindByIdAsync(userId);
            if (target == null)
            {
                return NotFound();
            }

            // Неможливо скинути власний пароль тут (використовуйте сторінку ChangePassword)
            var current = await _userManager.GetUserAsync(User);
            if (current != null && current.Id == target.Id)
            {
                TempData["ErrorMessage"] = "Власний пароль змінюйте через 'Змінити пароль'.";
                return RedirectToPage();
            }

            var tempPass = GenerateTempPassword();
            var token = await _userManager.GeneratePasswordResetTokenAsync(target);
            var result = await _userManager.ResetPasswordAsync(target, token, tempPass);
            if (result.Succeeded)
            {
                await _userManager.SetLockoutEndDateAsync(target, DateTimeOffset.UtcNow); // розблокування
                TempData["SuccessMessage"] = $"Тимчасовий пароль для {target.Email}: {tempPass}"; // показуємо одноразово
            }
            else
            {
                TempData["ErrorMessage"] = string.Join("; ", result.Errors.Select(e => e.Description));
            }

            return RedirectToPage();
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
            if (string.IsNullOrEmpty(userId))
            {
                return NotFound();
            }

            var target = await _userManager.FindByIdAsync(userId);
            if (target == null)
            {
                return NotFound();
            }

            // Забороняємо видаляти себе
            var current = await _userManager.GetUserAsync(User);
            if (current != null && current.Id == target.Id)
            {
                TempData["ErrorMessage"] = "Ви не можете видалити свій власний обліковий запис.";
                return RedirectToPage();
            }

            // Не дозволяємо видалити останнього адміністратора
            if (await _userManager.IsInRoleAsync(target, "Admin"))
            {
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                if (admins.Count <= 1)
                {
                    TempData["ErrorMessage"] = "Неможливо видалити останнього адміністратора.";
                    return RedirectToPage();
                }
            }

            try
            {
                // Видаляємо зовнішні логіни
                var logins = await _userManager.GetLoginsAsync(target);
                foreach (var l in logins)
                {
                    await _userManager.RemoveLoginAsync(target, l.LoginProvider, l.ProviderKey);
                }

                // Видаляємо claims
                var claims = await _userManager.GetClaimsAsync(target);
                foreach (var c in claims)
                {
                    await _userManager.RemoveClaimAsync(target, c);
                }

                // Видаляємо ролі
                var roles = await _userManager.GetRolesAsync(target);
                if (roles.Count > 0)
                {
                    await _userManager.RemoveFromRolesAsync(target, roles);
                }

                var result = await _userManager.DeleteAsync(target);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = $"Користувача {target.Email} успішно видалено.";
                    return RedirectToPage();
                }
                else
                {
                    TempData["ErrorMessage"] = string.Join("; ", result.Errors.Select(e => e.Description));
                    return RedirectToPage();
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Помилка при видаленні користувача: " + ex.Message;
                return RedirectToPage();
            }
        }

        // NEW: Unlock user (clear lockout)
        public async Task<IActionResult> OnPostUnlockUserAsync(string userId)
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

            // Clear lockout
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow);
            await _userManager.ResetAccessFailedCountAsync(user);
            TempData["SuccessMessage"] = $"Розблоковано {user.Email}.";
            return RedirectToPage();
        }
    }
}
