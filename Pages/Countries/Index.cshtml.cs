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
    public class IndexModel : PageModel
    {
        private readonly Melodies25.Data.Melodies25Context _context;

        public IndexModel(Melodies25.Data.Melodies25Context context)
        {
            _context = context;
        }

        public IList<Country> Country { get;set; } = default!;

        public async Task OnGetAsync()
        {
            Country = await _context.Country.ToListAsync();

            
            foreach (var currentCountry in Country)
            {
                int melodycount = 0;
                var authors = await _context.Author.Where(m => m.CountryID == currentCountry.ID).ToListAsync();
                currentCountry.AuthorsCount = authors.Count;

                
                foreach (var currentAuthor in authors)
                {
                    var meloides = await _context.Melody.Where(m => m.AuthorID == currentAuthor.ID).ToListAsync();
                    currentAuthor.MelodiesCount = meloides.Count;
                    melodycount += (int)currentAuthor.MelodiesCount;
                }
                currentCountry.MelodiesCount = melodycount;
            }
        }
    }
}
