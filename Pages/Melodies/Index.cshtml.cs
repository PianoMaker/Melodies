using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Melodies25.Data;
using Melodies25.Models;

namespace Melodies25.Pages.Melodies
{
    public class IndexModel : PageModel
    {
        private readonly Melodies25.Data.Melodies25Context _context;

        public IndexModel(Melodies25.Data.Melodies25Context context)
        {
            _context = context;
        }

        public IList<Melody> Melody { get;set; } = default!;

        public async Task OnGetAsync()
        {
            Melody = await _context.Melody
                .Include(m => m.Author).ToListAsync();
        }


        public IActionResult OnPostPlay(string path)
        {
            TempData["Message"] = $"Method for playing the file {path} is under development.";
            return RedirectToPage();
        }
    }
}
