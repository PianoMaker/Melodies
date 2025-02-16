using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Melodies25.Data;
using Melodies25.Models;
using static Music.Messages;
using static Melodies25.Utilities.PrepareFiles;
using Music;
using System.IO;
using Microsoft.Extensions.Hosting;
using Melodies25.Utilities;


namespace Melodies25.Pages.Melodies
{
    public class DetailsModel : PageModel
    {
        private readonly Melodies25.Data.Melodies25Context _context;
        private readonly IWebHostEnvironment _environment;

        public DetailsModel(Melodies25.Data.Melodies25Context context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public Models.Melody Melody { get; set; } = default!;



        public async Task<IActionResult> OnGetAsync(int? id)
        {
            MessageL(Music.COLORS.yellow, "MELODY/DETAILS OnGet method");
            if (id == null)
            {
                return NotFound();
            }

            var melody = await _context.Melody
                .Include(m => m.Author)
                .FirstOrDefaultAsync(m => m.ID == id);

            if (melody == null)
            {
                return NotFound();
            }
            else
            {
                Melody = melody;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            MessageL(Music.COLORS.yellow, "MELODY/DETAILS OnPost method");

            if (id == null)
            {
                return NotFound();
            }

            var melody = await _context.Melody
                .Include(m => m.Author)
                .FirstOrDefaultAsync(m => m.ID == id);

            if (melody == null)
            {
                return NotFound();
            }

            if (string.IsNullOrEmpty(melody.Filepath))
            {
                return Page();
            }

            var wwwRootPath = Path.Combine(_environment.WebRootPath, "melodies");
            var path = Path.Combine(wwwRootPath, melody.Filepath);

            var midifile = MidiConverter.GetMidiFile(path);
            melody.MidiMelody = await MidiConverter.GetMelodyFromMidiAsync(midifile);

            Melody = melody; // Оновлюємо модель

            return Page();
        }


    }
}
