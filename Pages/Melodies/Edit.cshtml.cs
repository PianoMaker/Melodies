using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Melodies25.Data;
using Melodies25.Models;

namespace Melodies25.Pages.Melodies
{
    public class EditModel : PageModel
    {
        private readonly Melodies25.Data.Melodies25Context _context;

        public EditModel(Melodies25.Data.Melodies25Context context)
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

            var melody =  await _context.Melody.FirstOrDefaultAsync(m => m.ID == id);
            if (melody == null)
            {
                return NotFound();
            }
            Melody = melody;
           ViewData["AuthorID"] = new SelectList(_context.Author, "ID", "ID");
            return Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Attach(Melody).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MelodyExists(Melody.ID))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        private bool MelodyExists(int id)
        {
            return _context.Melody.Any(e => e.ID == id);
        }
    }
}
