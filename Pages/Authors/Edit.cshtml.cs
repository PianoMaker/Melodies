using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Melodies25.Data;
using Melodies25.Models;
using Microsoft.DotNet.Scaffolding.Shared;
using static Music.Messages;
using Melodies25.Utilities;

namespace Melodies25.Pages.Authors
{
    [Authorize(Roles = "Admin, Moderator")]
    public class EditModel : PageModel
    {
        private readonly Melodies25.Data.Melodies25Context _context;

        public EditModel(Melodies25.Data.Melodies25Context context)
        {
            _context = context;
        }

        [BindProperty]
        public Author Author { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {

            if (id == null)
            {
                return NotFound();
            }

            var author =  await _context.Author.FirstOrDefaultAsync(m => m.ID == id);
            if (author == null)
            {
                return NotFound();
            }
            Author = author;
            ViewData["CountryID"] = new SelectList(_context.Country, "ID", "NameUk");


            var meloides = await _context.Melody.Where(m => m.AuthorID == Author.ID).ToListAsync();
            Author.MelodiesCount = meloides.Count;

            return Page();
        }
         

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Attach(Author).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AuthorExists(Author.ID))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }
        /*
        public async Task<IActionResult> OnPostTranslitAsync()
        {
            MessageL(Music.COLORS.yellow, "AUTHOR/EDIT translit method");

            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (string.IsNullOrEmpty(Author.SurnameEn))
            {
                Author.SurnameEn = Translit.Transliterate(Author.Surname);
            }
            if (string.IsNullOrEmpty(Author.NameEn))
            {
                Author.NameEn = Translit.Transliterate(Author.NameUk);
            }


            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AuthorExists(Author.ID))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Page();
        }
        */
        private bool AuthorExists(int id)
        {
            return _context.Author.Any(e => e.ID == id);
        }
    }
}
