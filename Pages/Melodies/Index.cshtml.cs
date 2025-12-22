using Azure;
using Melodies25.Data;
using Melodies25.Models;
using Melodies25.Pages.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.General;
using Music;
using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using static Melodies25.Utilities.PrepareFiles;
using static Melodies25.Utilities.WaveConverter;
using static Music.Messages;
using static Music.MidiConverter;
using Melody = Melodies25.Models.Melody;

namespace Melodies25.Pages.Melodies
{
    public class IndexModel : PageModel
    {
        private readonly Melodies25Context _context;
        private readonly IWebHostEnvironment _environment;
        public string Msg { get; set; } = default!;
        public string Errormsg { get; set; } = default!;

        public string TitleSort { get; set; } = default!;
        public string AuthorSort { get; set; } = default!;
        public string CurrentSort { get; set; } = default!;

        public string? SelectedLetter { get; set; }

        // Pagination properties
        public int PageIndex { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int PageSize { get; } = 50;
        public int TotalCount { get; set; } = 0;

        public readonly UserManager<IdentityUser> _userManager;

        public IndexModel(Melodies25Context context, IWebHostEnvironment environment, UserManager<IdentityUser> userManager)
        {
            
            _context = context;
            _environment = environment;
            _userManager = userManager;
        }
        
             
        public IList<Melody> Melody { get; set; } = new List<Melody>();

        [BindProperty(SupportsGet = true)]
        public string SelectedLang { get; set; } = "uk";
                
        public async Task OnGetAsync(string sortOrder, string? letter, int? pageIndex)
        {
            MessageL(COLORS.yellow, $"MELODY/INDEX -  OnGET");

            // Ensure SelectedLang reflects current UI culture (so view can render NameEn/SurnameEn for English)
            try
            {
                var ui = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                SelectedLang = string.IsNullOrWhiteSpace(ui) ? "uk" : ui;
            }
            catch
            {
                SelectedLang = "uk";
            }

            TitleSort = sortOrder == "title_asc" ? "title_desc" : "title_asc";
            AuthorSort = sortOrder == "author_asc" ? "author_desc" : "author_asc";
            CurrentSort = sortOrder;

            SelectedLetter = letter;
            

            var melodiesQuery = _context.Melody.Include(m => m.Author).AsQueryable();

            // Apply letter filter (if any)
            if (!string.IsNullOrEmpty(letter))
            {
                melodiesQuery = melodiesQuery.Where(m => m.Title.StartsWith(letter));
            }

            // Count total items for pagination
            TotalCount = await melodiesQuery.CountAsync();
            PageIndex = pageIndex.HasValue && pageIndex.Value > 0 ? pageIndex.Value : 1;
            TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
            if (TotalPages < 1) TotalPages = 1;
            if (PageIndex > TotalPages) PageIndex = TotalPages;

            // Apply sorting
            melodiesQuery = sortOrder switch
            {
                "title_asc" => melodiesQuery.OrderBy(m => m.Title),
                "title_desc" => melodiesQuery.OrderByDescending(m => m.Title),
                "author_asc" => melodiesQuery.OrderBy(m => m.Author.Surname).ThenBy(m => m.Author.Name),
                "author_desc" => melodiesQuery.OrderByDescending(m => m.Author.Surname).ThenByDescending(m => m.Author.Name),
                _ => melodiesQuery.OrderBy(m => m.ID) // stable default ordering
            };

            // Apply paging
            Melody = await melodiesQuery
                .Skip((PageIndex - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
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

            // Оновлюємо модель для відображення (reset to first page)
            return RedirectToPage(new { pageIndex = 1 });
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

            // Redirect to first page after deletion
            return RedirectToPage(new { pageIndex = 1 });
        }



        // Add sortOrder to the Filter POST and include it in redirect so paging preserves current sort.
        public IActionResult OnPostFilterByLetter(string letter, string? sortOrder = null)
        {
            SelectedLetter = letter;
            // Redirect to GET carrying current sortOrder (if any) and reset to first page
            return RedirectToPage(new { letter = letter, sortOrder = sortOrder, pageIndex = 1 });
        }
    }
}
