using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using static Music.Messages;

namespace Melodies25.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly SignInManager<IdentityUser> _signInManager;

        public LoginModel(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, IEmailSender emailSender)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }
        }

        public void OnGet()
        {
            MessageL(Music.COLORS.yellow, "Login / OnGet method");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            MessageL(Music.COLORS.yellow, "Login / OnPostAsync method");

            if (!ModelState.IsValid)
                return Page();

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Користувача з таким email не існує.");
                return Page();
            }

            // Use CheckPasswordSignInAsync with the found user to avoid username/email mismatches
            var result = await _signInManager.CheckPasswordSignInAsync(user, Input.Password, lockoutOnFailure: true);

            if (result.Succeeded)
                return RedirectToPage("/Index");
            if (result.RequiresTwoFactor)
            {
                ModelState.AddModelError(string.Empty, "Потрібна двофакторна автентифікація.");
                return Page();
            }
            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Обліковий запис заблоковано.");
                return Page();
            }
            if (result.IsNotAllowed)
            {
                ModelState.AddModelError(string.Empty, "Вхід наразі не дозволено.");
                return Page();
            }

            ModelState.AddModelError(string.Empty, "Невірний пароль.");
            return Page();
        }

        public async Task<IActionResult> OnPostRecoverPasswordAsync()
        {
            MessageL(Music.COLORS.yellow, "OnPostRecoverPasswordAsync");
            if (Input == null || string.IsNullOrEmpty(Input.Email))
            {
                ModelState.AddModelError(string.Empty, "Введіть Email.");
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                return RedirectToPage("/Account/RecoverPasswordConfirmation");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new { area = "Identity", code = token, email = Input.Email },
                protocol: Request.Scheme);

            await _emailSender.SendEmailAsync(
                Input.Email,
                "Відновлення пароля",
                $"Для відновлення пароля перейдіть за посиланням: <a href='{callbackUrl}'>Відновити</a>.");

            return RedirectToPage("/Account/RecoverPasswordConfirmation");
        }

        public IActionResult OnPostSignIn()
        {
            return RedirectToPage($"/Account/Register");
        }
    }
}
