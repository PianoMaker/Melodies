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
using Azure;

namespace Melodies25.Pages.Melodies
{
    public class IndexModel : PageModel
    {
        private readonly Melodies25.Data.Melodies25Context _context;
        private readonly IWebHostEnvironment _environment;
        public string Msg { get; set; } = default!;

        public string Errormsg { get; set; } = default!;

        public string TitleSort { get; set; } = default!;
        public string AuthorSort { get; set; } = default!;
        public string CurrentSort { get; set; } = default!;



        public IndexModel(Melodies25.Data.Melodies25Context context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // Ensure MusicMelody is never null to avoid NullReferenceException in the Razor view
        public IList<Melody> Melody { get; set; } = new List<Melody>();



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
                "author_desc" => melodiesQuery.OrderByDescending(m => m.Author.Surname).ThenByDescending(m => m.Author.Name),
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
                Melody = new List<Melody>();
                return Page();
            }

            if (_environment == null)
            {
                ErrorMessage("Помилка: IWebHostEnvironment не ініціалізовано.");
                Melody = await _context.Melody.Include(m => m.Author).ToListAsync();
                return Page();
            }

            // Заповнюємо модель до обробки, щоб view завжди мала дані
            Melody = await _context.Melody.Include(m => m.Author).ToListAsync();

            var errors = new List<string>();
            var snapshot = Melody.ToList(); // працюємо зі знімком

            foreach (var melody in snapshot)
            {
                if (string.IsNullOrEmpty(melody.FilePath))
                    continue;

                try
                {
                    var path = Path.Combine(_environment.WebRootPath, "melodies", melody.FilePath);
                    var ifeligible = IfMonody(path);
                    if (ifeligible)
                    {
                        await PrepareMp3Async(_environment, melody.FilePath, true);
                        melody.IsFileEligible = true;
                    }
                    else
                    {
                        melody.IsFileEligible = false;
                    }
                }
                catch (Exception ex)
                {
                    // Лог, але НЕ повертаємося — продовжуємо обробку інших файлів
                    ErrorMessage($"\nНеможливо обробити файл: {melody.FilePath}");
                    GrayMessageL(ex.Message);
                    errors.Add($"{melody.Title}: {ex.Message}");
                    continue;
                }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                GrayMessageL($"SaveChanges failed: {ex.Message}");
                Errormsg = "Не вдалося зберегти зміни у базі даних.";
            }

            // Оновлюємо модель для відображення
            Melody = await _context.Melody.Include(m => m.Author).ToListAsync();

            if (errors.Count > 0)
                Errormsg = string.Join("; ", errors);

            return Page();
        }


        public async Task<IActionResult> OnPostKilldupesAsync()
        {
            var melodies = await _context.Melody
            .Include(m => m.Author)
            .ToListAsync();

            var duplicates = melodies
                .GroupBy(m => new { m.Title })
                .Where(g => g.Count() > 1) // Фільтруємо дублікати
                .ToList();

            MessageL(COLORS.yellow, $"Ready to delete {duplicates.Count} dupes");
            foreach (var duplicateGroup in duplicates)
            {
                var duplicateList = duplicateGroup.ToList();


                // Залишаємо перший елемент і видаляємо решту
                for (int i = 1; i < duplicateList.Count; i++)
                {
                    var melodyToDelete = duplicateList[i];
                    _context.Melody.Remove(melodyToDelete);
                }
            }



            // Зберігаємо зміни
            await _context.SaveChangesAsync();

            Melody = await _context.Melody.Include(m => m.Author).ToListAsync();

            // Після завершення операції можна редіректити назад
            return Page();
        }


        public async Task<IActionResult> OnPostFilterByLetter(string letter)
        {
            if (string.IsNullOrEmpty(letter) || letter.Length != 1 || !char.IsLetter(letter[0]))
            {
                Melody = await _context.Melody.Include(m => m.Author).ToListAsync();
                return Page();
            }

            MessageL(COLORS.yellow, $"MELODY/INDEX -  FilterByLetter {letter}");

            Melody = await _context.Melody.Include(m => m.Author).Where(m => m.Title.StartsWith(letter)).ToListAsync();

            return Page();
        }
    }
}

    
