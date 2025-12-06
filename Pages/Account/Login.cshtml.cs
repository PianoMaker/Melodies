using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, IEmailSender emailSender, ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = logger;
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
                _logger.LogWarning("Login failed: user not found for email {Email}", Input.Email);
                TempData["ErrorMessage"] = "Користувача з таким email не існує.";
                return Page();
            }

            // Diagnostic info
            TempData["Debug_UserId"] = user.Id;
            TempData["Debug_UserName"] = user.UserName ?? "<null>";
            TempData["Debug_EmailConfirmed"] = user.EmailConfirmed.ToString();
            TempData["Debug_HasPasswordHash"] = (!string.IsNullOrEmpty(user.PasswordHash)).ToString();
            _logger.LogInformation("Attempting login for userId={UserId}, userName={UserName}, emailConfirmed={EmailConfirmed}, hasPasswordHash={HasHash}",
                user.Id, user.UserName, user.EmailConfirmed, !string.IsNullOrEmpty(user.PasswordHash));

            // Quick direct password check (returns bool) to see if password verification works
            bool passwordValid = await _userManager.CheckPasswordAsync(user, Input.Password);
            TempData["Debug_PasswordValid"] = passwordValid.ToString();
            _logger.LogInformation("Password valid check for {Email}: {Valid}", Input.Email, passwordValid);

            if (!passwordValid)
            {
                // As a fallback, try PasswordSignIn with user.UserName (some configs rely on username)
                var fallback = await _signInManager.PasswordSignInAsync(user.UserName ?? Input.Email, Input.Password, isPersistent: false, lockoutOnFailure: true);
                _logger.LogInformation("Fallback PasswordSignInAsync result: {Result}", fallback);
                if (fallback.Succeeded)
                {
                    return RedirectToPage("/Index");
                }

                if (fallback.RequiresTwoFactor)
                {
                    ModelState.AddModelError(string.Empty, "Потрібна двофакторна автентифікація.");
                    return Page();
                }
                if (fallback.IsLockedOut)
                {
                    ModelState.AddModelError(string.Empty, "Обліковий запис заблоковано.");
                    return Page();
                }
                if (fallback.IsNotAllowed)
                {
                    ModelState.AddModelError(string.Empty, "Вхід наразі не дозволено.");
                    return Page();
                }

                ModelState.AddModelError(string.Empty, "Невірний пароль.");
                return Page();
            }

            // If password is valid, sign in directly with the user object
            await _signInManager.SignInAsync(user, isPersistent: false);
            _logger.LogInformation("User {Email} signed in.", Input.Email);
            return RedirectToPage("/Index");
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
