using Melodies25.Data;
using Melodies25.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Music;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using static Music.Messages;


namespace Melodies25.Pages.Countries
{
    public class IndexModel : PageModel
    {
        private readonly Melodies25Context _context;
                
        public IndexModel(Melodies25Context context)
        {
            _context = context;         
        }

        public IList<Country> Country { get;set; } = default!;


        [BindProperty(SupportsGet = true)]
        public string SelectedLang { get; set; } = "uk";

        public async Task OnGetAsync()
        {
            try
            {
                var ui = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                SelectedLang = string.IsNullOrEmpty(ui) ? "uk" : ui;
            }
            catch
            {
                SelectedLang = "uk";
            }


            Country = await _context.Country.ToListAsync();

            bool isEn = string.Equals(SelectedLang, "en", StringComparison.OrdinalIgnoreCase);

            MessageL(COLORS.yellow, $"COUNTRIES/INDEX -  OnGET");
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
