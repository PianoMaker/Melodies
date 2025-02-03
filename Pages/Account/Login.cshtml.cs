using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace Melodies25.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        public LoginModel(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            public string Email { get; set; }
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
    }
}
