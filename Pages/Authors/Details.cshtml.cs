using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Melodies25.Data;
using Melodies25.Models;

namespace Melodies25.Pages.Authors
{
    public class DetailsModel : PageModel
    {
        private readonly Melodies25.Data.Melodies25Context _context;

        public DetailsModel(Melodies25.Data.Melodies25Context context)
        {
            _context = context;
        }

        public Author Author { get; set; } = default!;
        public List<Melody> Melody { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var author = await _context.Author.FirstOrDefaultAsync(m => m.ID == id);
            var melody = await _context.Melody
                .Where(m => m.AuthorID == id)
                .ToListAsync();

            Melody = melody;

            if (author == null)
            {
                return NotFound();
            }
            else
            {
                Author = author;

                var melodies = await _context.Melody.Where(m => m.AuthorID == author.ID).ToListAsync();
                author.MelodiesCount = melodies.Count;
            }
            return Page();
        }
    }
}
