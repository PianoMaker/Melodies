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
using System.Diagnostics;

namespace Melodies25.Pages.Melodies
{
    [Authorize(Roles = "Admin, Moderator")]
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
                .Include(m => m.Author)
                .FirstOrDefaultAsync(m => m.ID == id);

            if (melody == null)
            {
                return NotFound();
            }

            Melody = melody;

            // Add file existence check and error handling
            if (Melody.FilePath is not null)
            {
                var midiFilepath = Path.Combine(_environment.WebRootPath, "melodies", Melody.FilePath);
                
                try
                {
                    if (!System.IO.File.Exists(midiFilepath))
                    {
                        // Log the missing file
                        MessageL(COLORS.red, $"MIDI file not found: {midiFilepath}");
                        // Set a warning message that will be displayed to the user
                        TempData["ErrorWarning"] = "MIDI файл не знайдено на сервері";
                        // Set default tempo
                        Tempo = 120; // Default tempo if file is missing
                    }
                    else
                    {
                        Tempo = (int)GetTempofromMidi(midiFilepath);
                    }
                }
                catch (Exception ex)
                {
                    // Log the error
                    MessageL(COLORS.red, $"Error reading MIDI file: {ex.Message}");
                    // Set a user-friendly error message
                    TempData["ErrorWarning"] = "Помилка читання MIDI файлу";
                    // Set default tempo
                    Tempo = 120;
                }
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

        //private async Task GetMidiMelody(Melody melody)
        //{
        //    MidiFile midifile = GetMidiFile(melody);
        //    Globals.lng = Music.LNG.uk;
        //    Globals.notation = Music.Notation.eu;
        //    melody.MidiMelody = await MidiConverter.GetMelodyFromMidiAsync(midifile);
        //}

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
            MessageL(COLORS.yellow, "MELODIES/DETECT TONALITY");
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

        // AJAX: change to parallel (relative) tonality and persist
        public async Task<JsonResult> OnGetParallelTonalityAsync(int id)
        {
            MessageL(COLORS.yellow, "MELODIES/RELATIVE TONALITY starts");
            try
            {

                var sw = Stopwatch.StartNew();
                var melody = await _context.Melody.FirstOrDefaultAsync(m => m.ID == id);
                if (melody == null)
                    return new JsonResult(new { ok = false, error = "melody_not_found" });

                if (string.IsNullOrWhiteSpace(melody.Tonality))
                    return new JsonResult(new { ok = false, error = "no_tonality" });

                var tonStr = (melody.Tonality ?? "").Trim()
                    .Replace('\u2013','-').Replace('\u2014','-').Replace('\u2212','-')
                    .Replace('–','-').Replace('—','-').Replace('−','-')
                    .Replace("  "," ");
                MessageL(COLORS.gray, $"[normalized tonality='{tonStr}' ({sw.ElapsedMilliseconds} ms)");

                var current = new Music.Tonalities(tonStr);
                MessageL(COLORS.gray, $"parsed Tonalities ({sw.ElapsedMilliseconds} ms)");

                if (current.Mode == Music.MODE.dur) current.Transport(6); else current.Transport(3);
                MessageL(COLORS.gray, $"after transport ({sw.ElapsedMilliseconds} ms)");

                int sf = current.Keysignatures();
                int idx = sf + 7;
                string[] majors = { "Ces","Ges","Des","As","Es","B","F","C","G","D","A","E","H","Fis","Cis" };
                string[] minors = { "as","es","b","f","c","g","d","a","e","h","fis","cis","gis","dis","ais" };
                string name = current.Mode == Music.MODE.dur
                    ? (idx >= 0 && idx < majors.Length ? majors[idx] : current.Name())
                    : (idx >= 0 && idx < minors.Length ? minors[idx] : current.Name());

                var newTonality = $"{name}-{(current.Mode == Music.MODE.dur ? "dur" : "moll")}";
                MessageL(COLORS.purple, $"new tonality is {newTonality}");

                melody.Tonality = newTonality;
                _context.Attach(melody);
                _context.Entry(melody).Property(x => x.Tonality).IsModified = true;
                await _context.SaveChangesAsync();
                MessageL(COLORS.green, $"parsed Tonalities ({sw.ElapsedMilliseconds} ms)");

                return new JsonResult(new { ok = true, tonality = newTonality });
            }
            catch (Exception ex)
            {
                MessageL(COLORS.red, $"error: {ex}");
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

            foreach (var prop in new[] { "Title", "Year", "AuthorID", "Description", "IsFileEligible", "Tonality" })
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

            // ОНОВЛЕННЯ Key Signature за вибраною тональністю при натисканні "Зберегти"
            try
            {
                if (!string.IsNullOrWhiteSpace(Melody.Tonality) && !string.IsNullOrWhiteSpace(Melody.FilePath))
                {
                    var fullPath = Path.Combine(_environment.WebRootPath, "melodies", Melody.FilePath);
                    MessageL(COLORS.olive, $"[CALL APPLYKS] {Melody.Tonality} -> {Melody.FilePath}");
                    ApplyKeySignatureWierd(fullPath, Melody.Tonality);
                }
                else
                {
                    GrayMessageL("No tonality selected; Key Signature update skipped");
                }
            }
            catch (Exception ex)
            {
                ErrorMessageL($"Failed to update Key Signature: {ex.Message}");
            }

            return RedirectToPage("./Details", new { id = Melody.ID });
        }

        // у OnPostUpdate перед викликом
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
                    // Update MIDI key signature based on selected tonality
                    if (!string.IsNullOrWhiteSpace(melody.Tonality))
                    {
                        var fullPath = Path.Combine(_environment.WebRootPath, "melodies", melody.FilePath);
                        MessageL(COLORS.olive, $"[CALL APPLYKS] {melody.Tonality} -> {melody.FilePath}");
                        ApplyKeySignatureWierd(fullPath, melody.Tonality);
                    }

                    await PrepareMp3Async(_environment, melody.FilePath, false); // генеруємо з копією всередині
                }
                catch (Exception e)
                {
                    ErrorMessageL(e.Message);

                }
            }

            return RedirectToPage("Details", new { id = melody.ID });
        }

        private void ApplyKeySignature(string fullPath, string tonality)
        {
            MessageL(COLORS.olive, $"[APPLYKS ENTER] path={fullPath}, inputTon={tonality}");
            if (!System.IO.File.Exists(fullPath)) { GrayMessageL($"MIDI not found: {fullPath}"); return; }

            var tonStr = (tonality ?? "").Trim()
                .Replace('\u2013','-').Replace('\u2014','-').Replace('\u2212','-')
                .Replace('–','-').Replace('—','-').Replace('−','-')
                .Replace("  "," ");
            var tonal = new Music.Tonalities(tonStr);
            int sharps = tonal.Keysignatures();
            var mode = tonal.Mode;

            MessageL(COLORS.olive, $"ApplyKeySignature: {tonStr} -> sf={sharps}, mode={mode}");

            var midi = new MidiFile(fullPath);
            var newMidiCollection = InsertKeySignatures(midi, sharps, mode);

            // Обхідний перезапис тим самим іменем
            ExportOverwrite(fullPath, newMidiCollection);

            var verified = GetTonalities(new MidiFile(fullPath));
            if (verified != null)
                MessageL(COLORS.gray, $"[APPLYKS EXIT] verified sf={verified.GetSharpFlats()}, mode={verified.Mode}");
            else
                MessageL(COLORS.darkred, "[APPLYKS EXIT] verified: not found");
        }

        private void ApplyKeySignatureWierd(string fullPath, string tonality)
        {
            // Байтовий патч Key Signature: шукаємо FF 59 02 і переписуємо sf/mi під тим самим ім'ям файлу
            try
            {
                MessageL(COLORS.olive, $"[APPLYKS-WIERD ENTER] path={fullPath}, inputTon={tonality}");
                if (!System.IO.File.Exists(fullPath)) { GrayMessageL($"MIDI not found: {fullPath}"); return; }

                // Нормалізація введеної тональності
                var tonStr = (tonality ?? "").Trim()
                    .Replace('\u2013','-').Replace('\u2014','-').Replace('\u2212','-')
                    .Replace('–','-').Replace('—','-').Replace('−','-')
                    .Replace("  "," ");
                var tonal = new Music.Tonalities(tonStr);
                int sharps = tonal.Keysignatures(); // -7..+7
                var mode = tonal.Mode;               // MODE.dur / MODE.moll
                byte mi = (byte)(mode == Music.MODE.moll ? 1 : 0);
                byte sfByte = unchecked((byte)(sbyte)sharps);
                MessageL(COLORS.olive, $"[APPLYKS-WIERD TARGET] sf={sharps}, mi={mi}");

                // Читаємо байти файлу
                var attrs = System.IO.File.GetAttributes(fullPath);
                if ((attrs & System.IO.FileAttributes.ReadOnly) != 0)
                    System.IO.File.SetAttributes(fullPath, attrs & ~System.IO.FileAttributes.ReadOnly);

                byte[] bytes = System.IO.File.ReadAllBytes(fullPath);

                // Логуємо наявні KS у байтах до патчу
                int foundBefore = 0;
                for (int i = 0; i + 4 < bytes.Length; i++)
                {
                    if (bytes[i] == 0xFF && bytes[i + 1] == 0x59 && bytes[i + 2] == 0x02)
                    {
                        sbyte oldSf = unchecked((sbyte)bytes[i + 3]);
                        byte oldMi = bytes[i + 4];
                        MessageL(COLORS.gray, $"[APPLYKS-WIERD BEFORE] pos=0x{i:X}, sf={oldSf}, mi={oldMi}");
                        foundBefore++;
                    }
                }
                if (foundBefore == 0)
                {
                    MessageL(COLORS.yellow, "[APPLYKS-WIERD] No KeySignature meta-events (FF 59 02) found to patch");
                }

                // Патчимо всі входження FF 59 02
                int replaced = 0;
                for (int i = 0; i + 4 < bytes.Length; i++)
                {
                    if (bytes[i] == 0xFF && bytes[i + 1] == 0x59 && bytes[i + 2] == 0x02)
                    {
                        bytes[i + 3] = sfByte;
                        bytes[i + 4] = mi;
                        replaced++;
                    }
                }

                // Записуємо назад тим самим ім'ям
                System.IO.File.WriteAllBytes(fullPath, bytes);
                MessageL(COLORS.olive, $"[APPLYKS-WIERD WRITE] replaced={replaced}");

                // Логуємо після запису
                int foundAfter = 0;
                var bytesAfter = System.IO.File.ReadAllBytes(fullPath);
                for (int i = 0; i + 4 < bytesAfter.Length; i++)
                {
                    if (bytesAfter[i] == 0xFF && bytesAfter[i + 1] == 0x59 && bytesAfter[i + 2] == 0x02)
                    {
                        sbyte newSf = unchecked((sbyte)bytesAfter[i + 3]);
                        byte newMi = bytesAfter[i + 4];
                        MessageL(COLORS.gray, $"[APPLYKS-WIERD AFTER] pos=0x{i:X}, sf={newSf}, mi={newMi}");
                        foundAfter++;
                    }
                }

                // Додаткова верифікація через NAudio-парсер
                var verified = GetTonalities(new MidiFile(fullPath));
                if (verified != null)
                {
                    MessageL(COLORS.gray, $"[APPLYKS-WIERD EXIT] verified sf={verified.GetSharpFlats()}, mode={verified.Mode}, patched={replaced}, ksCountBefore={foundBefore}, ksCountAfter={foundAfter}");
                }
                else
                {
                    MessageL(COLORS.darkred, $"[APPLYKS-WIERD EXIT] verified: not found, patched={replaced}, ksCountBefore={foundBefore}, ksCountAfter={foundAfter}");
                }
            }
            catch (Exception ex)
            {
                ErrorMessageL($"[APPLYKS-WIERD ERROR] {ex.Message}");
            }
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
    }
}
