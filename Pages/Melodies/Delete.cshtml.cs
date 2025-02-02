using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Melodies25.Data;
using Melodies25.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Melodies25.Pages.Melodies
{
    //[Authorize(Roles = "Admin")] - це повне вимкнення
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
        public Melody Melody { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            /*м'яке посилання користува*/
            var user = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            if (!isAdmin)
            {
                return RedirectToPage("/Shared/AccessDenied"); // Перенаправлення на сторінку з відмовою в доступі
            }
            /**/

            var melody = await _context.Melody.FirstOrDefaultAsync(m => m.ID == id);

            if (melody == null)
            {
                return NotFound();
            }
            else
            {
                Melody = melody;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var melody = await _context.Melody.FindAsync(id);
            if (melody != null)
            {
                Melody = melody;
                _context.Melody.Remove(Melody);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
