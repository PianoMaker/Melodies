using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Melodies25.Data;
using Melodies25.Models;
using static Music.MidiConverter;
using static Music.Messages;
using static SynthWaveProvider;
using NAudio.Midi;
using System.IO;
using Music;
using Melody = Melodies25.Models.Melody;

namespace Melodies25.Pages.Melodies
{
    public class IndexModel : PageModel
    {
        private readonly Melodies25.Data.Melodies25Context _context;
        private readonly IWebHostEnvironment _environment;
        public string Msg { get; set; }

        public string Errormsg { get; set; }

        public IndexModel(Melodies25.Data.Melodies25Context context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
            Msg = "сторінку завантажено";
            Errormsg = "->";
        }

        public IList<Melody> Melody { get;set; } = default!;

        

        public async Task OnGetAsync()
        {
            Melody = await _context.Melody
                .Include(m => m.Author).ToListAsync();

            MessageL(COLORS.yellow, "Index OnGet");

        }


        public IActionResult OnPostPlay(string midiPath)
        {

            MessageL(COLORS.yellow, "Index OnPost");
            MessageL(COLORS.green, $"Trying to get {midiPath}");
            try
            {
                var path = Path.Combine(_environment.WebRootPath, "melodies", midiPath);
                var midiFile = new MidiFile(path);
                var hzmslist = GetHzMsListFromMidi(midiFile);

                string mp3Path = ConvertToMp3Path(path);
                MessageL(COLORS.green, $"Starting to prepare {mp3Path}");

                GenerateMp3(hzmslist, mp3Path);
                var relativePath = "/mp3/" + Path.GetFileName(mp3Path);
                TempData["AudioFile"] = relativePath;
                MessageL(COLORS.green, relativePath);
            }
            catch (Exception ex)
            {
                Errormsg = ex.Message;
                ErrorMessage($"Неможливо згенерувати MP3:\n {ex.Message}\n");
            }
            return RedirectToPage();
        }


        private string ConvertToMp3Path(string midiPath)
        {
            
            string directory = Path.GetDirectoryName(midiPath)?.Replace("melodies", "mp3") ?? "";
            //string directory = Path.Combine(_environment.WebRootPath, directory);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            string filenameWithoutExt = Path.GetFileNameWithoutExtension(midiPath);
            return Path.Combine(directory, filenameWithoutExt + ".mp3");
        }
    }
}
