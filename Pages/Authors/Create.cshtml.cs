using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Melodies25.Data;
using Melodies25.Models;

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
        public IActionResult OnGet()
        {

            if (string.IsNullOrEmpty(SelectedMode))
            {
                SelectedMode = "composer"; // Значення за замовчуванням
            }

            ViewData["CountryID"] = new SelectList(_context.Country, "ID", "Name");
            return Page();
        }

        [BindProperty]
        public Author Author { get; set; } = default!;

        // For more information, see https://aka.ms/RazorPagesCRUD.
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
