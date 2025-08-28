using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Melodies25.Pages.Account
{
    public class RecoverPasswordModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;

        public RecoverPasswordModel(UserManager<IdentityUser> userManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        [BindProperty]
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();
            var user = await _userManager.FindByEmailAsync(Email);
            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
            {
                TempData["PwdResetMsg"] = "якщо email ≥снуЇ та п≥дтверджений Ц лист над≥слано.";
                return Page();
            }
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.Page("/Account/ResetPassword", null, new { email = Email, code = token }, Request.Scheme);
            await _emailSender.SendEmailAsync(Email, "—киданн€ парол€", $"ѕерейд≥ть за посиланн€м дл€ скиданн€: <a href='{callbackUrl}'>—кинути пароль</a>");
            TempData["PwdResetMsg"] = "Ћист дл€ скиданн€ парол€ над≥слано.";
            return Page();
        }
    }
}
