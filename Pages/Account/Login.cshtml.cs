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

            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                ModelState.AddModelError(string.Empty, "Email не підтверджений. Перевірте пошту.");
                return Page();
            }

            var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, false, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                return RedirectToPage("/Index");
            }
            if (result.RequiresTwoFactor)
            {
                ModelState.AddModelError(string.Empty, "Потрібна двофакторна автентифікація.");
                return Page();
            }
            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Обліковий запис заблоковано через багато невдалих спроб. Спробуйте пізніше.");
                return Page();
            }
            if (result.IsNotAllowed)
            {
                ModelState.AddModelError(string.Empty, "Вхід заборонено для цього облікового запису.");
                return Page();
            }

            // Generic wrong password case
            ModelState.AddModelError(string.Empty, "Невірний пароль.");
            return Page();
        }

        public async Task<IActionResult> OnPostRecoverPasswordAsync()
        {
            MessageL(Music.COLORS.yellow, "OnPostRecoverPasswordAsync");
            if (Input == null || string.IsNullOrEmpty(Input.Email))
            {
                ModelState.AddModelError(string.Empty, "Вкажіть Email.");
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
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
                "Скидання пароля",
                $"Для скидання пароля перейдіть за посиланням: <a href='{callbackUrl}'>Скинути пароль</a>.");

            return RedirectToPage("/Account/RecoverPasswordConfirmation");
        }

        public async Task<IActionResult> OnPostSignIn()
        {
            return RedirectToPage($"/Account/Register");

        }


    }
}
