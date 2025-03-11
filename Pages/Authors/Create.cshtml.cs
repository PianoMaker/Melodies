using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Melodies25.Data;
using Melodies25.Models;
using Microsoft.EntityFrameworkCore;

namespace Melodies25.Pages.Authors
{
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

            if (string.IsNullOrEmpty(SelectedMode))
            {
                SelectedMode = "composer"; // Значення за замовчуванням
            }

            ViewData["CountryID"] = new SelectList(_context.Country, "ID", "Name");
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
            ViewData["CountryID"] = new SelectList(_context.Country, "ID", "Name");

            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Author.Add(Author);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
