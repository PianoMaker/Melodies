using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace Melodies25.Pages.Account
{
    [Authorize]
    public class ProfileModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;

        public ProfileModel(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        public string Email { get; set; }
        public string UserName { get; set; }
        public string Role { get; set; }

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                Email = user.Email;
                UserName = user.UserName;

                var roles = await _userManager.GetRolesAsync(user);

                Role = roles.Count > 0 ? string.Join(", ", roles) : "Немає ролі";
            }
        }
    }
}
