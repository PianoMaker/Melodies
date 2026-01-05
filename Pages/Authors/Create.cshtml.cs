using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using static Music.Messages;
using Melodies25.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.DotNet.Scaffolding.Shared;
using Music;
using Microsoft.AspNetCore.Authorization;

namespace Melodies25.Pages.Authors
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly Melodies25.Data.Melodies25Context _context;

        public CreateModel(Melodies25.Data.Melodies25Context context)
        {
            _context = context;
        }

        [BindProperty]
        public string? SelectedMode { get; set; }

        [BindProperty]
        public Author Author { get; set; } = default!;
        public IActionResult OnGet()
        {
            MessageL(COLORS.yellow, "Authors/CREATE OnGet");
            if (string.IsNullOrEmpty(SelectedMode))
            {
                SelectedMode = "composer"; // Значення за замовчуванням
            }
            var countries = _context.Country.ToList();
            if (countries == null || !countries.Any())
            {
                countries = new List<Country> { new Country { ID = 0, Name = "-- Оберіть країну --" } };
            }           

            ViewData["CountryID"] = new SelectList(_context.Country, "ID", "Name");
            if (ViewData["CountryID"] == null)
            {
                ViewData["CountryID"] = new SelectList(Enumerable.Empty<SelectListItem>());
            }
            return Page();
        }

        public async Task<IActionResult> OnGetCheckAuthorAsync(string author)
        {
            Console.WriteLine($"Checking for author {author}");
            bool exists = await _context.Author.AnyAsync(m => m.Surname == author || m.SurnameEn == author);
            return new JsonResult(new { exists });
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ViewData["CountryID"] = new SelectList(_context.Country, "ID", "GetName");
            MessageL(COLORS.yellow, "Authors/CREATE OnGet");
            if (string.IsNullOrEmpty(SelectedMode))
            {
                SelectedMode = "composer"; // Значення за замовчуванням
            }
            var countries = _context.Country.ToList();
            if (countries == null || !countries.Any())
            {
                countries = new List<Country> { new Country { ID = 0, Name = "-- Оберіть країну --" } };
            }

            if (!ModelState.IsValid)
            {
                ViewData["CountryID"] = new SelectList(_context.Country, "ID", "Name");
                if (ViewData["CountryID"] == null)
                {
                    ViewData["CountryID"] = new SelectList(Enumerable.Empty<SelectListItem>());
                }
                return Page();
            }

            _context.Author.Add(Author);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
