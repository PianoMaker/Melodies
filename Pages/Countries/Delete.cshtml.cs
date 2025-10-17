using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Melodies25.Data;
using Melodies25.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace Melodies25.Pages.Countries
{
    [Authorize(Roles = "Admin, Moderator")]
    public class DeleteModel : PageModel
    {
        private readonly Melodies25.Data.Melodies25Context _context;
        
        private readonly UserManager<IdentityUser> _userManager;
        public DeleteModel(Melodies25.Data.Melodies25Context context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public Country Country { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            /*м'яке посилання користувача */
            bool isAdminOrModerator;
            var user = await _userManager.GetUserAsync(User);
            if (user is not null)
            { isAdminOrModerator = await _userManager.IsInRoleAsync(user, "Admin") || await _userManager.IsInRoleAsync(user, "Moderator"); }
            else isAdminOrModerator = false;


            if (!isAdminOrModerator)
            {
                return RedirectToPage("/Shared/AccessDenied"); 
            }

            /**/
            var country = await _context.Country.FirstOrDefaultAsync(m => m.ID == id);

            if (country == null)
            {
                return NotFound();
            }
            else
            {
                Country = country;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var country = await _context.Country.FindAsync(id);
            if (country != null)
            {
                Country = country;
                _context.Country.Remove(Country);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
