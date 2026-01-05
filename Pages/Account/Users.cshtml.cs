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
    [Authorize(Roles = "Admin")]  // ������ ���� ��� �������������
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

        // ����� OnGetAsync ��� ������������ ������������ �� �� �����
        public async Task<IActionResult> OnGetAsync()
        {
                  // �1��� ���������
                  var user = await _userManager.GetUserAsync(User);
                  if (user == null || !await _userManager.IsInRoleAsync(user, "Admin"))
                  {
                      return RedirectToPage("/Shared/AccessDenied");
                  }
            
            // ����������� ������ ������������ �� �����
            Users = _userManager.Users.ToList();
            Roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

            // �������� �� ��������� �����
            if (Users == null || Roles == null)
            {
                ModelState.AddModelError(string.Empty, "���� �� ������ ���� �����������.");
                return Page();
            }

            UserRoles = new Dictionary<string, List<string>>();

            // ����������� ��� ��� ������� �����������
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

        // �������� ������
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

            // ��������� ������� ������� ������ ��� (�������������� ������� ChangePassword)
            var current = await _userManager.GetUserAsync(User);
            if (current != null && current.Id == target.Id)
            {
                TempData["ErrorMessage"] = "������� ������ ������� ����� '������ ������'.";
                return RedirectToPage();
            }

            var tempPass = GenerateTempPassword();
            var token = await _userManager.GeneratePasswordResetTokenAsync(target);
            var result = await _userManager.ResetPasswordAsync(target, token, tempPass);
            if (result.Succeeded)
            {
                await _userManager.SetLockoutEndDateAsync(target, DateTimeOffset.UtcNow); // �������������
                TempData["SuccessMessage"] = $"���������� ������ ��� {target.Email}: {tempPass}"; // �������� ����������
            }
            else
            {
                TempData["ErrorMessage"] = string.Join("; ", result.Errors.Select(e => e.Description));
            }

            return RedirectToPage();
        }

        // ��������� ��� ������������� �� �����������
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
                TempData["SuccessMessage"] = $"{user.Email} ������� ���� �������������.";
                return RedirectToPage();
            }

            ModelState.AddModelError(string.Empty, "������� ��� ����������� ���.");
            return RedirectToPage();
        }

        // ��������� ��� ������������� �� �����������
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
                ModelState.AddModelError("", "��������� �������� ���������� �������������.");
                return Page();
            }

            var result = await _userManager.RemoveFromRoleAsync(user, "Admin");
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"{user.Email} ������� ���� �������������.";
                return RedirectToPage();
            }

            ModelState.AddModelError("", "�� ������� �������� ���� �������������.");
            return Page();
        }

        // ϳ����������� ���������� �����
        public async Task<IActionResult> OnPostConfirmEmailAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"����������� � ID {userId} �� ��������.");
            }

            user.EmailConfirmed = true;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "���������� ����� ����������� ������.";
            }
            else
            {
                TempData["ErrorMessage"] = "������� ������� �� ��� ������������ ���������� �����.";
            }

            return RedirectToPage();
        }

        // ����������� ��� ���������� �����������
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
                TempData["SuccessMessage"] = $"{user.Email} ������� ���� ����������.";
            }
            else
            {
                TempData["ErrorMessage"] = "�� ������� ���������� ���� ����������.";
            }

            return RedirectToPage();
        }

        // ��������� ��� ���������� �� �����������
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
                TempData["SuccessMessage"] = $"{user.Email} ������� ���� ����������.";
            }
            else
            {
                TempData["ErrorMessage"] = "�� ������� �������� ���� ����������.";
            }

            return RedirectToPage();
        }

        // �������� ��� ��������� �����������
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

            // ����������� �������� ����
            var current = await _userManager.GetUserAsync(User);
            if (current != null && current.Id == target.Id)
            {
                TempData["ErrorMessage"] = "�� �� ������ �������� ��� ������� �������� �����.";
                return RedirectToPage();
            }

            // �� ���������� �������� ���������� �������������
            if (await _userManager.IsInRoleAsync(target, "Admin"))
            {
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                if (admins.Count <= 1)
                {
                    TempData["ErrorMessage"] = "��������� �������� ���������� �������������.";
                    return RedirectToPage();
                }
            }

            try
            {
                // ��������� �������� �����
                var logins = await _userManager.GetLoginsAsync(target);
                foreach (var l in logins)
                {
                    await _userManager.RemoveLoginAsync(target, l.LoginProvider, l.ProviderKey);
                }

                // ��������� claims
                var claims = await _userManager.GetClaimsAsync(target);
                foreach (var c in claims)
                {
                    await _userManager.RemoveClaimAsync(target, c);
                }

                // ��������� ���
                var roles = await _userManager.GetRolesAsync(target);
                if (roles.Count > 0)
                {
                    await _userManager.RemoveFromRolesAsync(target, roles);
                }

                var result = await _userManager.DeleteAsync(target);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = $"����������� {target.Email} ������ ��������.";
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
                TempData["ErrorMessage"] = "������� ��� ��������� �����������: " + ex.Message;
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
            TempData["SuccessMessage"] = $"������������ {user.Email}.";
            return RedirectToPage();
        }
    }
}
