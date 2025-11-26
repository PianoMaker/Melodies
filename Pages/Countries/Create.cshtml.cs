using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Melodies25.Data;
using Melodies25.Models;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using Microsoft.AspNetCore.Authorization;

namespace Melodies25.Pages.Countries
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly Melodies25.Data.Melodies25Context _context;

        public string Msg { get; set; } = default!;

        public CreateModel(Melodies25.Data.Melodies25Context context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public Country Country { get; set; } = default!;

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {

                _context.Country.Add(Country);
                await _context.SaveChangesAsync();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"помилка бази даних\n{ex}");
                Msg = $"Країна з такою назвою вже створена";
                return Page();
            }


            return RedirectToPage("./Index");
        }
    }
}
