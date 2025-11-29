using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Melodies25.Data;
using Melodies25.Models;

namespace Melodies25.Pages.Countries
{
    public class DetailsModel : PageModel
    {
        private readonly Melodies25.Data.Melodies25Context _context;

        public DetailsModel(Melodies25.Data.Melodies25Context context)
        {
            _context = context;
        }

        public Country Country { get; set; } = default!;
        public List<Author> Author { get; set; } = default!;

        // Один хендлер, який приймає nullable id.
        public async Task<IActionResult> OnGetAsync(int? id)
        {            
            if (id == null || id <= 0)
            {                
                return RedirectToPage("/Countries/Index");
            }

            var country = await _context.Country.FirstOrDefaultAsync(m => m.ID == id);
            var authors = await _context.Author.Where(a => a.CountryID == id).ToListAsync();

            foreach (var author in authors)
            {
                var melodies = await _context.Melody.Where(m => m.AuthorID == author.ID).ToListAsync();
                author.MelodiesCount = melodies.Count;
            }

            Author = authors;

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
    }
}
