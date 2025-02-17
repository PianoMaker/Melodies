using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Melodies25.Data;
using Melodies25.Models;
using Music;
using static Music.Messages;
using Microsoft.Identity.Client;

namespace Melodies25.Pages.Authors
{
    public class IndexModel : PageModel
    {
        private readonly Melodies25.Data.Melodies25Context _context;

        public IndexModel(Melodies25.Data.Melodies25Context context)
        {
            _context = context;
        }

        public IList<Author> Author { get;set; } = default!;
        
        public string SurnameSort { get; set; }
        public string CountrySort { get; set; }
        public string MelodiesCountSort { get; set; }

        public async Task OnGetAsync(string sortOrder)
        {
            MessageL(COLORS.yellow, $"AUTHORS/INDEX -  OnGET");

            SurnameSort = sortOrder == "surname_asc" ? "surname_desc" : "surname_asc";
            CountrySort = sortOrder == "country_asc" ? "country_desc" : "country_asc";
            MelodiesCountSort = sortOrder == "melody_asc" ? "melody_desc" : "melody_asc";

            var authorQuery = _context.Author.Include(m => m.Country).AsQueryable();

            // Отримуємо всі мелодії, пов'язані з авторами, за один запит
            var melodies = await _context.Melody.ToListAsync();

            // Збираємо кількість мелодій для кожного автора
            foreach (var currentAuthor in authorQuery)
            {
                currentAuthor.MelodiesCount = melodies.Count(m => m.AuthorID == currentAuthor.ID);
            }

            authorQuery = sortOrder switch
            {
                "surname_asc" => authorQuery.OrderBy(a => a.Surname == "Українська народна пісня" ? "!" + a.Surname : a.Surname),
                "surname_desc" => authorQuery.OrderByDescending(a => a.Surname == "Українська народна пісня" ? "!" + a.Surname : a.Surname),
                "country_asc" => authorQuery.OrderBy(a => a.Country.Name),
                "country_desc" => authorQuery.OrderByDescending(a => a.Country.Name),
                "melody_asc" => authorQuery.OrderBy(a => a.MelodiesCount),
                "melody_desc" => authorQuery.OrderByDescending(a => a.MelodiesCount),
                _ => authorQuery // Якщо немає сортування, залишаємо список без змін
            };

            Author = await authorQuery.ToListAsync();
        }

    }
}
