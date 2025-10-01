using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Melodies25.Data;
using Melodies25.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authorization;
using System;
using Microsoft.AspNetCore.Identity;
using Melodies25.Utilities;
using Music;
using NAudio.Midi;
using static Music.Messages;
using static Music.MidiConverter;
using static Melodies25.Utilities.PrepareFiles;
using Melody = Melodies25.Models.Melody;
using System.IO;

namespace Melodies25.Pages.Melodies
{
    public class EditModel : PageModel
    {
        private readonly Melodies25.Data.Melodies25Context _context;
        private readonly IWebHostEnvironment _environment;
        private readonly UserManager<IdentityUser> _userManager;


        public EditModel(Melodies25.Data.Melodies25Context context, IWebHostEnvironment hostEnvironment, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _environment = hostEnvironment;
            _userManager = userManager;
        }

        [BindProperty]
        public Melody Melody { get; set; } = default!;

        [BindProperty]
        public int Tempo { get; set; }

        [BindProperty]
        public bool Tempocorrected { get; set; } = false;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            MessageL(COLORS.yellow, "MELODIES/EDIT OnGet");


            if (id == null)
            {
                return NotFound();
            }

            var melody = await _context.Melody
                .Include(m => m.Author).
                FirstOrDefaultAsync(m => m.ID == id);
            if (melody == null)
            {
                return NotFound();
            }
            Melody = melody;
            if (Melody.FilePath is not null)
            {
                var midiFilepath = Path.Combine(_environment.WebRootPath, "melodies", Melody.FilePath);
                Tempo = (int)GetTempofromMidi(midiFilepath);
            }

            GrayMessageL($"tempocorrected = {Tempocorrected}, FilePath = {Melody.FilePath}, Tempo = {Tempo}");
            ViewData["AuthorID"] = new SelectList(_context.Author.OrderBy(a => a.Surname), "ID", "Surname");
            ViewData["Tonalities"] = new SelectList(new List<string>
            {
                "C-dur", "G-dur", "D-dur", "A-dur", "E-dur", "H-dur", "Fis-dur", "Cis-dur",
                "F-dur", "B-dur", "Es-dur", "As-dur", "Des-dur", "Ges-dur", "Ces-dur",
                "a-moll", "e-moll", "h-moll", "fis-moll", "cis-moll", "gis-moll", "dis-moll", "ais-moll",
                "d-moll", "g-moll", "c-moll", "f-moll", "b-moll", "es-moll", "as-moll"
            });            

            return Page();
        }

        private async Task GetMidiMelody(Melody melody)
        {
            MidiFile midifile = GetMidiFile(melody);
            Globals.lng = Music.LNG.uk;
            Globals.notation = Music.Notation.eu;
            melody.MidiMelody = await MidiConverter.GetMelodyFromMidiAsync(midifile);
        }

        private MidiFile GetMidiFile(Melody melody)
        {
            var wwwRootPath = Path.Combine(_environment.WebRootPath, "melodies");
            var path = Path.Combine(wwwRootPath, melody.FilePath);
            var midifile = MidiConverter.GetMidiFile(path);
            return midifile;
        }

        // AJAX: Detect tonality from MIDI Key Signature meta-event and persist to DB
        public async Task<JsonResult> OnGetDetectTonalityAsync(int id)
        {
            try
            {
                var melody = await _context.Melody.FirstOrDefaultAsync(m => m.ID == id);
                if (melody == null)
                {
                    return new JsonResult(new { ok = false, error = "melody_not_found" });
                }
                if (string.IsNullOrWhiteSpace(melody.FilePath))
                {
                    return new JsonResult(new { ok = false, error = "no_file" });
                }
                var fullPath = Path.Combine(_environment.WebRootPath, "melodies", melody.FilePath);
                var tonality = MidiKeySignatureDetector.TryDetectTonality(fullPath);
                if (string.IsNullOrWhiteSpace(tonality))
                {
                    return new JsonResult(new { ok = false, error = "no_keysig" });
                }

                // Persist detected tonality
                melody.Tonality = tonality;
                _context.Attach(melody);
                _context.Entry(melody).Property(x => x.Tonality).IsModified = true;
                await _context.SaveChangesAsync();

                return new JsonResult(new { ok = true, tonality });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { ok = false, error = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostAsync(IFormFile? fileupload)
        {
            MessageL(COLORS.yellow, "MELODIES/EDIT OnPost");
            if (Melody is null) ErrorMessageL("Melody is null");
            else GrayMessageL($"tempocorrected = {Tempocorrected}, FilePath = {Melody.FilePath}, Tempo = {Tempo}");

            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    ErrorMessage(error.ErrorMessage);
                }
            }


            var uploadsPath = Path.Combine(_environment.WebRootPath, "melodies");
            Directory.CreateDirectory(uploadsPath);

            // завантаження файлу
            if (fileupload is not null)
            {
                Melody.Author = await _context.Author.FirstOrDefaultAsync(a => a.ID == Melody.AuthorID);

                string authorPart = Melody.Author is not null
                    ? $"{Translit.Transliterate(Melody.Author.Surname)}_"
                    : "";

                string newfilename = $"{authorPart}{Translit.Transliterate(Melody.Title)}.mid";
                string midiFilePath = Path.Combine(uploadsPath, newfilename);

                using (var stream = new FileStream(midiFilePath, FileMode.Create))
                {
                    await fileupload.CopyToAsync(stream);
                }

                ViewData["Message"] = "Файл успішно завантажено!";
                Melody.FilePath = newfilename;
            }
            else
            {
                Console.WriteLine("fileupload is null");
            }

            _context.Attach(Melody).State = EntityState.Modified;


            //створення аудіо
            if (Melody.FilePath is not null && Tempocorrected == false)
            {
                await PrepareAudio(uploadsPath);
            }

            foreach (var prop in new[] { "Title", "Year", "AuthorID", "Description", "IsFileEligible" })
            {
                _context.Entry(Melody).Property(prop).IsModified = true;
            }

            _context.Entry(Melody).Property(m => m.FilePath).IsModified = fileupload is not null;

            try
            {
                var result = await _context.SaveChangesAsync();
                if (result > 0)
                {
                    MessageL(COLORS.green, $"{result} rows updated");
                }
                else
                {
                    MessageL(COLORS.yellow, "No rows were updated.");
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MelodyExists(Melody.ID))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            //корекція темпу
            if (Melody.FilePath is not null && Tempocorrected == true)
            {

                string fullPath = Path.Combine(_environment.WebRootPath, "melodies", Melody.FilePath);
                MessageL(COLORS.purple, $"Перезапис файлу, темп = {Tempo}");

                if (System.IO.File.Exists(fullPath))
                {
                    try
                    {
                        var newmidifile = new MidiFile(fullPath);
                        UpdateTempoInMidiFile(newmidifile, Tempo);
                        MidiFile.Export(fullPath, newmidifile.Events);
                        MessageL(COLORS.purple, $"Midi-файл перезаписано, темп - {GetTempofromMidi(fullPath)}");
                        await PrepareAudio(uploadsPath);

                    }
                    catch (Exception e)
                    {
                        ErrorMessage("Не вдалося перезаписати файл: ");
                        GrayMessageL(e.Message);
                    }

                }
            }
            else
            {
                GrayMessageL($"Темп незмінний: {Tempo}");
            }

            return RedirectToPage("./Details", new { id = Melody.ID });
        }

        private async Task PrepareAudio(string uploadsPath)
        {
            MessageL(COLORS.olive, $"PrepareAudio method, path = {uploadsPath}");
            try
            {
                string originalMidiPath = Path.Combine(uploadsPath, Melody.FilePath);
                if (!System.IO.File.Exists(originalMidiPath))
                {
                    ErrorMessage("Файл не існує");
                    return;
                }

                // Робоча копія ТІЛЬКИ у temporary з унікальним префіксом, щоб не конфліктувати
                string tempDir = Path.Combine(_environment.WebRootPath, "temporary");
                Directory.CreateDirectory(tempDir);
                string workPath = Path.Combine(tempDir, "_check_" + Path.GetFileName(Melody.FilePath));
                System.IO.File.Copy(originalMidiPath, workPath, true);

                var midiFile = new MidiFile(workPath);
                int changed = 0;
                StraightMidiFile(workPath, ref changed); // працюємо тільки з копією

                var ifeligible = IfMonody(workPath); // перевірка копії

                if (ifeligible)
                {
                    MessageL(COLORS.standart, $"генеруємо mp3 з оригінала (не змінюючи його), тимчасові копії всередині PrepareMp3Async");
                    try
                    {
                        // Генерація MP3 на основі оригінального імені файлу (без префіксів)
                        await PrepareMp3Async(_environment, Melody.FilePath, false);
                        ViewData["Message"] = "Файл успішно завантажено!";
                        Melody.IsFileEligible = true;
                    }
                    catch
                    {
                        ViewData["Message"] = "Не вдалося згенерувати файл";
                    }
                }
                else
                {
                    ViewData["Message"] = "Файл не є мелодією";
                    Melody.IsFileEligible = false;
                }

                // Видаляємо робочу копію з temporary
                if (System.IO.File.Exists(workPath))
                {
                    System.IO.File.Delete(workPath);
                    MessageL(COLORS.cyan, "Temporary working copy deleted");
                }
            }
            catch (Exception ex)
            {
                ErrorMessage($"failed to check file: {ex}");
            }
        }

        public async Task<IActionResult> OnPostDeleteFileAsync()
        {
            MessageL(COLORS.yellow, "MELODIES/EDIT OnPostDeleteFile");
            var melody = await _context.Melody.FindAsync(Melody.ID);
            if (melody == null)
            {
                return NotFound();
            }


            /*м'яке посилання користувача */
            bool isAdminOrModerator;
            var user = await _userManager.GetUserAsync(User);
            if (user is not null)
            { isAdminOrModerator = await _userManager.IsInRoleAsync(user, "Admin") || await _userManager.IsInRoleAsync(user, "Moderator"); }
            else isAdminOrModerator = false;


            if (!isAdminOrModerator)
            {
                return RedirectToPage("/Shared/AccessDenied");
            }

            /**/
            /**/


            if (!string.IsNullOrEmpty(melody.FilePath))
            {
                string filePath = Path.Combine(_environment.WebRootPath, "uploads", melody.FilePath);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                melody.FilePath = null; // Очистити шлях у БД
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Edit", new { id = melody.ID });
        }

        private bool MelodyExists(int id)
        {
            return _context.Melody.Any(e => e.ID == id);
        }



        public async Task<IActionResult> OnPostUpdate(int? id)
        {
            MessageL(Music.COLORS.yellow, "MELODY/EDIT OnPostUpdate method");

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

            if (melody.FilePath is not null)
            {
                try
                {
                    await PrepareMp3Async(_environment, melody.FilePath, false); // генеруємо з копією всередині
                }
                catch (Exception e)
                {
                    ErrorMessageL(e.Message);

                }
            }

            return RedirectToPage("Details", new { id = melody.ID });
        }
    }
}
