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
using Newtonsoft.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace Melodies25.Pages.Melodies
{
    public class DetailsModel : PageModel
    {
        private readonly Melodies25.Data.Melodies25Context _context;
        private readonly IWebHostEnvironment _environment;
        public string Labels { get; set; }
        public string Values { get; set; }

        public string WeightValues { get; set; }

        public string ErrorMsg { get; set; }

        public DetailsModel(Melodies25.Data.Melodies25Context context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public Models.Melody Melody { get; set; } = default!;

        public Dictionary<string, float> Degrees;
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
            Globals.lng = Music.LNG.uk;
            Globals.notation = Music.Notation.eu;
            melody.MidiMelody = await MidiConverter.GetMelodyFromMidiAsync(midifile);
            if (melody.Tonality != null)
            {
                melody.MidiMelody.Tonality = new Tonalities(melody.Tonality);
            }
            melody.MidiMelody.Enharmonize();           

            Melody = melody; // Оновлюємо модель

            // графік
            try
            {
                var data = melody.MidiMelody.GetDegreesStats();
                var weightdata = melody.MidiMelody.GetDegreesWeightStats();
                if (data is not null && weightdata is not null)
                {
                    Labels = JsonConvert.SerializeObject(data.Keys.ToList());
                    Values = JsonConvert.SerializeObject(data.Values.ToList());
                    WeightValues = JsonConvert.SerializeObject(weightdata.Values.ToList());
                }
                else ErrorMessageL("Check if tonality is set.");
            }
            catch (Exception ex)
            {
                ErrorMessageL("impossible to build charts.");
                MessageL(COLORS.standart, ex.Message);
                ErrorMsg = ex.Message;

            }

            return Page();
        }       

    }
}
