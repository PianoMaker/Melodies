using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Melodies25.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;

        public LoginModel(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager,IEmailSender emailSender)
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

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {

                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user != null && !await _userManager.IsEmailConfirmedAsync(user))
                {
                    ModelState.AddModelError(string.Empty, "You must confirm your email before logging in.");
                    Console.WriteLine("User is not confirmed");
                    return Page();
                }
                var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, false, false);



                if (result.Succeeded)
                {
                    return RedirectToPage("/Index"); // Перехід після входу
                }

                if (result.IsLockedOut)
                {
                    Console.WriteLine("Account is locked out.");
                }
                else if (result.RequiresTwoFactor)
                {
                    Console.WriteLine("Two-factor authentication is required.");
                }
                else
                {
                    Console.WriteLine("Invalid login attempt. Here are some possible reasons:");
                    Console.WriteLine($"IsLockedOut: {result.IsLockedOut}");
                    Console.WriteLine($"RequiresTwoFactor: {result.RequiresTwoFactor}");
                    Console.WriteLine($"IsNotAllowed: {result.IsNotAllowed}");                    
                }
                               

                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostRecoverPassword()
        {
            Console.WriteLine("Recovering password");
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    // Не показуємо, чи існує користувач, щоб уникнути зловживань
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
                    "Reset Password",
                    $"Please reset your password by <a href='{callbackUrl}'>clicking here</a>.");




                return RedirectToPage("/Account/RecoverPasswordConfirmation");
            }
            return Page();
        }
    }
}
