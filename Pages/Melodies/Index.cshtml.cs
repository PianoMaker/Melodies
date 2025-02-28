using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Melodies25.Data;
using Melodies25.Models;
using static Music.MidiConverter;
using static Music.Messages;
using static Melodies25.Utilities.PrepareFiles;
using static Melodies25.Utilities.WaveConverter;
using NAudio.Midi;
using System.IO;
using Music;
using Melody = Melodies25.Models.Melody;
using Microsoft.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;

namespace Melodies25.Pages.Melodies
{
    public class IndexModel : PageModel
    {
        private readonly Melodies25.Data.Melodies25Context _context;
        private readonly IWebHostEnvironment _environment;
        public string Msg { get; set; }

        public string Errormsg { get; set; }

        public string TitleSort { get; set; }
        public string AuthorSort { get; set; }
        public string CurrentSort { get; set; }



        public IndexModel(Melodies25.Data.Melodies25Context context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public IList<Melody> Melody { get; set; } = default!;



        public async Task OnGetAsync(string sortOrder)
        {
            MessageL(COLORS.yellow, $"MELODY/INDEX -  OnGET");

            TitleSort = sortOrder == "title_asc" ? "title_desc" : "title_asc";
            AuthorSort = sortOrder == "author_asc" ? "author_desc" : "author_asc";
            CurrentSort = sortOrder;

            var melodiesQuery = _context.Melody.Include(m => m.Author).AsQueryable();

            melodiesQuery = sortOrder switch
            {
                "title_asc" => melodiesQuery.OrderBy(m => m.Title),
                "title_desc" => melodiesQuery.OrderByDescending(m => m.Title),
                "author_asc" => melodiesQuery.OrderBy(m => m.Author.Surname).ThenBy(m => m.Author.Name),
                "author_desc" => melodiesQuery.OrderByDescending(m => m.Author.Name).ThenByDescending(m => m.Author.Surname),
                _ => melodiesQuery // Якщо немає сортування, залишаємо список без змін
            };

            Melody = await melodiesQuery.ToListAsync();

        }

        public async Task<IActionResult> OnPostAsync()
        {
            MessageL(COLORS.yellow, $"MELODY/INDEX -  OnPOST");

            if (_context?.Melody == null)
            {
                ErrorMessage("Помилка: База даних недоступна.");
                return Page();
            }

            if (_environment == null)
            {
                ErrorMessage("Помилка: IWebHostEnvironment не ініціалізовано.");
                return Page();
            }

            var melodiesQuery = _context.Melody.ToList();

            foreach (var melody in melodiesQuery)
            {
                if (!string.IsNullOrEmpty(melody.Filepath))
                {
                    try
                    {
                        var path = Path.Combine(_environment.WebRootPath, "melodies", melody.Filepath);
                        var ifeligible = IfMonody(path);
                        if (ifeligible)
                        {
                            await PrepareMp3Async(_environment, melody.Filepath, true);
                            melody.IsFileEligible = true;
                        }
                        else
                        {
                            melody.IsFileEligible = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorMessage("\nНеможливо обробити файл:");
                        GrayMessageL(ex.Message);
                        return Page();
                    }
                }
            }

            await _context.SaveChangesAsync(); // Винесли з циклу

            Melody = await _context.Melody.Include(m => m.Author).ToListAsync();

            return Page();
        }

    }
}


