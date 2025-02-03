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

namespace Melodies25.Pages.Authors
{
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
        public Author Author { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            /*м'яке посилання користувача */
            var user = await _userManager.GetUserAsync(User);
            if (user is not null)
            {
                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                if (!isAdmin)
                {
                    return RedirectToPage("/Shared/AccessDenied");
                }
            }
            else return RedirectToPage("/Shared/AccessDenied");
            /**/

            var author = await _context.Author.FirstOrDefaultAsync(m => m.ID == id);

            if (author == null)
            {
                return NotFound();
            }
            else
            {
                Author = author;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var author = await _context.Author.FindAsync(id);
            if (author != null)
            {
                Author = author;
                _context.Author.Remove(Author);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
