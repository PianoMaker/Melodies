using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Melodies25.Data;
using Melodies25.Models;

namespace Melodies25.Pages.Melodies
{
    public class DeleteModel : PageModel
    {
        private readonly Melodies25.Data.Melodies25Context _context;

        public DeleteModel(Melodies25.Data.Melodies25Context context)
        {
            _context = context;
        }

        [BindProperty]
        public Melody Melody { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

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
