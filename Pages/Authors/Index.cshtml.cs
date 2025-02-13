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
        

        public async Task OnGetAsync()
        {
            MessageL(COLORS.yellow, $"AUTHORS/INDEX -  OnGET");
            Author = await _context.Author.Include(m=>m.Country).ToListAsync();

            foreach(var currentAuthor in Author) {
                var meloides = await _context.Melody.Where(m => m.AuthorID == currentAuthor.ID).ToListAsync();
                currentAuthor.MelodiesCount = meloides.Count;
            }


        }
    }
}
