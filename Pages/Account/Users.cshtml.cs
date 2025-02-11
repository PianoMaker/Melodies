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
    [Authorize(Roles = "Admin")]  // ������ ���� ��� ������������
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

            // �������� �� �������� �����
            if (Users == null || Roles == null)
            {
                ModelState.AddModelError(string.Empty, "��� �� ������ ���� ����������.");
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

        // ��������� ��� ������������ �� �����������
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
                TempData["SuccessMessage"] = $"{user.Email} ������� ���� ������������.";
                return RedirectToPage();
            }

            ModelState.AddModelError(string.Empty, "������� ��� ���������� ���.");
            return RedirectToPage();
        }

        // ��������� ��� ������������ �� �����������
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
                ModelState.AddModelError("", "��������� �������� ���������� ������������.");
                return Page();
            }

            var result = await _userManager.RemoveFromRoleAsync(user, "Admin");
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"{user.Email} ������� ���� ������������.";
                return RedirectToPage();
            }

            ModelState.AddModelError("", "�� ������� �������� ���� ������������.");
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
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    // ������ ���������
                    return RedirectToPage();
                }
            }
            return BadRequest("�� ������� �������� �����������.");
        }
    }
}
