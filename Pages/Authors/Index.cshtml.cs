using Melodies25.Data;
using Melodies25.Models;
using Melodies25.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using Music;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Music.Messages;
using System.Globalization;

namespace Melodies25.Pages.Authors
{
    public class IndexModel : PageModel
    {
        private readonly Melodies25Context _context;        

       // explicitly pick this ctor for DI
        public IndexModel(Melodies25Context context)
        {
            _context = context;            
        }
               

        public IList<Author> Author { get; set; } = default!;
                
        public string SurnameSort { get; set; }
        
        public string CountrySort { get; set; }
        
        public string MelodiesCountSort { get; set; }

        [BindProperty(SupportsGet = true)] 
        public string SelectedLang { get; set; } = "uk";


        public async Task OnGetAsync(string sortOrder)
        {
            MessageL(COLORS.yellow, $"AUTHORS/INDEX -  OnGET");

            // Read current UI culture set by RequestLocalization (e.g. via cookie from _Layout toggle)
            try
            {
                var ui = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                SelectedLang = string.IsNullOrEmpty(ui) ? "uk" : ui;
            }
            catch
            {
                SelectedLang = "uk";
            }

            await LoadPage(sortOrder);
        }

        public IActionResult OnPost()
        {
            SelectedLang = string.IsNullOrEmpty(SelectedLang) ? "uk" : (SelectedLang == "en" ? "en" : "uk");
            // PRG: зберігаємо вибір мови в query, щоб він не губився на переходах (у т.ч. сортування)
            return RedirectToPage(new { SelectedLang = SelectedLang });
        }

        private async Task LoadPage(string sortOrder)
        {
            // Toggle sort states
            SurnameSort = sortOrder == "surname_asc" ? "surname_desc" : "surname_asc";
            CountrySort = sortOrder == "country_asc" ? "country_desc" : "country_asc";
            MelodiesCountSort = sortOrder == "melody_asc" ? "melody_desc" : "melody_asc";

            // Determine language from SelectedLang which is set on OnGet from CurrentUICulture
            bool isEn = string.Equals(SelectedLang, "en", StringComparison.OrdinalIgnoreCase);
            string folkUk = "Українська народна пісня";
            string folkEn = "Ukrainian folk song";

            // Base query: join with melodies to get count (COUNT done in SQL, no NotMapped sorting issue)
            var baseQuery =
                from a in _context.Author.Include(a => a.Country)
                join m in _context.Melody on a.ID equals m.AuthorID into g
                select new
                {
                    Author = a,
                    MelodyCount = g.Count()
                };

            // Apply sorting
            baseQuery = sortOrder switch
            {
                "surname_asc" => isEn
                    ? baseQuery.OrderBy(x =>
                        ((x.Author.SurnameEn ?? x.Author.Surname) == folkEn
                            ? "!" + (x.Author.SurnameEn ?? x.Author.Surname)
                            : (x.Author.SurnameEn ?? x.Author.Surname)))
                    : baseQuery.OrderBy(x =>
                        (x.Author.Surname == folkUk ? "!" + x.Author.Surname : x.Author.Surname)),

                "surname_desc" => isEn
                    ? baseQuery.OrderByDescending(x =>
                        ((x.Author.SurnameEn ?? x.Author.Surname) == folkEn
                            ? "!" + (x.Author.SurnameEn ?? x.Author.Surname)
                            : (x.Author.SurnameEn ?? x.Author.Surname)))
                    : baseQuery.OrderByDescending(x =>
                        (x.Author.Surname == folkUk ? "!" + x.Author.Surname : x.Author.Surname)),

                "country_asc" => isEn
                    ? baseQuery.OrderBy(x => x.Author.Country != null ? (x.Author.Country.NameEn ?? "") : "")
                    : baseQuery.OrderBy(x => x.Author.Country != null ? (x.Author.Country.Name ?? "") : ""),

                "country_desc" => isEn
                    ? baseQuery.OrderByDescending(x => x.Author.Country != null ? (x.Author.Country.NameEn ?? "") : "")
                    : baseQuery.OrderByDescending(x => x.Author.Country != null ? (x.Author.Country.Name ?? "") : ""),

                "melody_asc"  => baseQuery.OrderBy(x => x.MelodyCount),
                "melody_desc" => baseQuery.OrderByDescending(x => x.MelodyCount),

                _ => baseQuery
            };

            // Materialize and copy MelodyCount back into NotMapped property
            var rows = await baseQuery.ToListAsync();
            foreach (var row in rows)
            {
                row.Author.MelodiesCount = row.MelodyCount; // safe: NotMapped
            }

            Author = rows.Select(r => r.Author).ToList();
        }

        public async Task OnPostTranslitAsync(string sortOrder)
        {
            MessageL(COLORS.yellow, "AUTHOR/ONPOSTTRANSLIT");

            SelectedLang = Request.Cookies["site_lang"] ?? SelectedLang ?? "uk";

            await LoadPage(sortOrder);

            foreach (var author in Author)
            {
                if (string.IsNullOrEmpty(author.NameEn) && !string.IsNullOrEmpty(author.Name))
                {
                    var nameEn = Translit.Transliterate(author.Name);
                    author.NameEn = nameEn;
                }

                if (string.IsNullOrEmpty(author.SurnameEn) && !string.IsNullOrEmpty(author.Surname))
                {
                    var surnameEn = Translit.Transliterate(author.Surname);
                    author.SurnameEn = surnameEn;
                }

                await _context.SaveChangesAsync();
            }

            Page();
        }
    }
}
