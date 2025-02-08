using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Melodies25.Models;
using System.Threading.Tasks;
using static Music.Messages;

namespace Melodies25.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        public RegisterModel(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
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
                var user = new IdentityUser { UserName = Input.Email, Email = Input.Email };
                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    //Автоматично входимо якщо зареєструвалися

                    var login = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, false, false);

                    if (login.Succeeded)
                    {
                     MessageL(Music.COLORS.green, "logged in as " + Input.Password);
                        return RedirectToPage("/Index"); // Перехід після входу
                    }

                    if (login.IsLockedOut)
                    {
                        ErrorMessage("Account is locked out.");
                    }
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                    ErrorMessage(error.Description);
                }
            }

            return Page();
        }
    }
}
