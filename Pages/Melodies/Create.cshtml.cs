using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Melodies25.Models;
using Melodies25.Utilities;
using Music;
using static Music.Messages;
using static Music.MidiConverter;
using static Melodies25.Utilities.PrepareFiles;
using static Melodies25.Utilities.WaveConverter;
using Microsoft.EntityFrameworkCore;
using Melody = Melodies25.Models.Melody;
using System.Text.Json;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;

namespace Melodies25.Pages.Melodies
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly Melodies25.Data.Melodies25Context _context;

        private readonly IWebHostEnvironment _environment;

        private readonly ILogger<CreateModel> _logger;

        

        public int? SelectedAuthorID { get; set; } = default!;

        [BindProperty]
        public bool ShowAuthor { get; set; } = default!;

        [BindProperty]
        public string TempAuthor { get; set; } = default!;

        [BindProperty]
        public Models.Melody Melody { get; set; } = default!;

        public string Msg { get; set; } = default!;

        [BindProperty]
        public string? SelectedMode { get; set; } = default!;

        public string? ErrorWarning { get; set; } = default!;

        /*Для роботи MelodyForm*/
        [BindProperty]
        public string Keys { get; set; } = default!;

        public MusicMelody NewPattern { get; set; }
        internal string TempMidiFilePath { get; set; }

        [BindProperty]
        internal string TempMp3FilePath { get; set; }

        private static readonly char[] separator = new char[] { ' ', '_' };

        private bool isPrivileged;

        public CreateModel(Data.Melodies25Context context, IWebHostEnvironment environment, ILogger<CreateModel> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
            //NewPattern = new MusicMelody();
        }

        public IActionResult OnGet(int selectedAuthorId)
        {

            MessageL(COLORS.yellow, "MELODIES/CREATE OnGet");

            if (TempData["ErrorWarning"] is not null)
            {
                ErrorWarning = TempData["ErrorWarning"] as string;
            }

            if (Melody == null)
                Melody = new Melody();

            if (selectedAuthorId > 0)
                Melody.AuthorID = selectedAuthorId;
            
            GetAuthorsData();
            GetTonalitiesData();

            ShowAuthor = false;
            return Page();
        }

        //Формування спадного списку тональностей
        private void GetTonalitiesData()
        {
            ViewData["Tonalities"] = new SelectList(new List<string>
            {
                "C-dur", "G-dur", "D-dur", "A-dur", "E-dur", "H-dur", "Fis-dur", "Cis-dur",
                "F-dur", "B-dur", "Es-dur", "As-dur", "Des-dur", "Ges-dur", "Ces-dur",
                "a-moll", "e-moll", "h-moll", "fis-moll", "cis-moll", "gis-moll", "dis-moll", "ais-moll",
                "d-moll", "g-moll", "c-moll", "f-moll", "b-moll", "es-moll", "as-moll"
            });
        }

        //Формування спадного списку авторів
        private void GetAuthorsData()
        {
            ViewData["AuthorID"] = new SelectList(_context.Author
                                                        .OrderBy(a => a.Surname)
                                                        .Select(a => new
                                                        {
                                                            a.ID,
                                                            FullName = a.Name + " " + a.Surname  // Об'єднуємо Name і Surname
                                                        }),
                                                    "ID",
                                                    "FullName",
                                                    Melody?.AuthorID
                                                );
        }

        // СТВОРЕННЯ МЕЛОДІЇ ВРУЧНУ //
        //зберігає в тимчасовий midi та mp3
        //викликається з CreateMelody.js
        //
        public async Task<IActionResult> OnPostMelody()
        {
            MessageL(COLORS.yellow, $"MELODIES/CREATE - OnPostMelody method, keys = {Keys}");
            if (TempData["ErrorWarning"] is not null)
            {
                ErrorWarning = TempData["ErrorWarning"] as string; GrayMessageL("generating errormessage");
            }
            else GrayMessageL("errormessage is null");
            //if (TempData["Title"] is not null)
            //    MusicMelody.Title = TempData["Title"] as string;

            GetAuthorsData();
            GetTonalitiesData();
            SaveKeys();
            
            try
            {
                await PrepareMp3Async(_environment, TempMidiFilePath, false);
                TempMp3FilePath = GetTemporaryPath(ConvertToMp3Path(TempMidiFilePath));
                TempData["HighlightPlayButton"] = true;
            }
            catch (Exception)
            {
                TempData["ErrorWarning"] = "Не вдалося згенерувати файл";
            }

            
            TempData.Keep("Title");
            ViewData["AuthorID"] = new SelectList(_context.Author.OrderBy(a => a.Surname), "ID", "Surname", SelectedAuthorID);
            TempData["TempMidiFilePath"] = TempMidiFilePath;
            TempData["Keys"] = Keys;

            MessageL(COLORS.gray, $"VD title = {TempData["Title"]}, VD Keys = {TempData["Keys"]}");            
            MessageL(COLORS.cyan, "OnPostMelody is finished");
            return Page();
        }

        //створює нове MIDI на основі введених нот 
        private void SaveKeys()
        {
            if (Keys is not null)
            {
                /* Будуємо послідовність введених нот */
                MusicMelody MelodyPattern = new();
                Globals.notation = Notation.eu;
                Globals.lng = LNG.uk;
                BuildPattern(MelodyPattern);
                NewPattern = (MusicMelody)MelodyPattern.Clone();

                /* Створює MIDI в диеркторію TempMidiFilePath на основі введеної послідовності */
                try
                {

                    TempMidiFilePath = PrepareTempName(_environment, ".mid");
                    MelodyPattern.SaveMidi(TempMidiFilePath);
                    MessageL(COLORS.green, "file is ready");
                    Msg = $"file is ready, path = {TempMidiFilePath}";
                    MessageL(COLORS.gray, $"Keys = {TempData["Keys"]}");
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"Проблема з доступом до файлу: {ex.Message}");
                    TempData["ErrorWarning"] = $"Проблема з доступом до файлу";
                }
                catch (Exception e)
                {
                    ErrorMessageL($"Failed to create MIDI file: {e.ToString()}");
                    TempData["ErrorWarning"] = $"Невідома помилка";
                    Console.WriteLine($"Проблема: {e.Message}");
                }
            }
            else
            {
                ErrorMessageL("keys are null");
            }
        }

        //читання нотного рядку відбувається по одній ноті у конструкторі Note(string input) 
        private void BuildPattern(MusicMelody MelodyPattern)
        {
            var pattern = Keys.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            foreach (var key in pattern)
            {
                try
                {
                    var note = new Note(key);
                    MelodyPattern.AddNote(note);
                }
                catch
                {
                    ErrorMessageL($"impossible to read note {key}\n");
                }
            }
        }

        public async Task OnPostAsync(string key)
        {
            //подолання глюку
            MessageL(COLORS.yellow, $"MELODIES/CREATE - OnPostAsync method {key}");
            GetAuthorsData();
            GetTonalitiesData();
            OnPostPiano(key);
            try
            {
                await PrepareMp3Async(_environment, TempMidiFilePath, false);
                TempMp3FilePath = ConvertToMp3Path(TempMidiFilePath);
                GrayMessageL($"TempMp3FilePath = {TempMp3FilePath}");
            }
            catch
            {
                TempData["ErrorMessage"] = "Не вдалося згенерувати файл";
            }
        }

        // перевірка чи існує файл
        public JsonResult OnGetCheckFileExists(int authorId, string title)
        {
            var author = _context.Author.FirstOrDefault(a => a.ID == authorId);
            if (author == null)
                return new JsonResult(false);

            string filename = $"{Translit.Transliterate(author.Surname)}_{Translit.Transliterate(title)}.mid";
            var fullPath = Path.Combine(_environment.WebRootPath, "melodies", filename);
            bool exists = System.IO.File.Exists(fullPath);
            return new JsonResult(exists);
        }

        /* ЗАПИС НОВОЇ МЕЛОДІЇ НА СЕРВЕР АБО В JSON */
        public async Task<IActionResult> OnPostCreateAsync(IFormFile? fileupload)
        {

            MessageL(COLORS.yellow, "MELODIES/CREATE OnPostAsync");

            // тест на валідність моделі
            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogError("MELODIES/CREATE OnPostAsync " + error.ErrorMessage);
                }
                //return Page();
            }

            TempMidiFilePath = TempData["TempMidiFilePath"] as string ?? "";


            Melody.Author = await _context.Author
    .FirstOrDefaultAsync(a => a.ID == Melody.AuthorID);

            if (Melody.Author == null)
            {
                Console.WriteLine("Author has not been not found!");
                return Page(); // або обробити помилку іншим чином
            }


            // Define both folders once
            var incomingDir = Path.Combine(_environment.WebRootPath, "melodies", "incoming");
            var publishedDir = Path.Combine(_environment.WebRootPath, "melodies");

            Directory.CreateDirectory(incomingDir);
            Directory.CreateDirectory(publishedDir);


            // determine privilege at request time (do not rely on constructor)
            bool isPrivileged = User?.Identity?.IsAuthenticated == true
                                && (User.IsInRole("Admin") || User.IsInRole("Moderator"));

            string savedMidifilePath = string.Empty;
            string savedFileName = string.Empty;

            //ЯКЩО ФАЙЛ ЗАВАНТАЖЕНО ЧЕРЕЗ ФОРМУ
            
            if (fileupload is not null)
            {
                //складаємо ім'я файлу
                string newfilename = $"{Translit.Transliterate(Melody.Author.Surname)}_{Translit.Transliterate(Melody.Title)}.mid";
                Melody.FilePath = newfilename; //назву MIDI файлу фіксуємо (назва для DB/JSON)

                MessageL(COLORS.green, $"try to process uploaded file {newfilename}");

                // choose destination based on role: privileged -> published, others -> incoming
                var destDir = isPrivileged ? publishedDir : incomingDir;
                var midifilePath = Path.Combine(destDir, newfilename);

                // ensure unique name in destination
                var baseName = Path.GetFileNameWithoutExtension(newfilename);
                var ext = Path.GetExtension(newfilename);
                int k = 0;
                while (System.IO.File.Exists(midifilePath))
                {
                    k++;
                    var candidate = $"{baseName}_{k}{ext}";
                    midifilePath = Path.Combine(destDir, candidate);
                    Melody.FilePath = Path.GetFileName(midifilePath);
                }

                using (var stream = new FileStream(midifilePath, FileMode.Create))
                {
                    await fileupload.CopyToAsync(stream);
                }

                savedMidifilePath = midifilePath;
                savedFileName = Path.GetFileName(midifilePath);

                // встановлюємо тональність, якщо її не вказано
                if (string.IsNullOrWhiteSpace(Melody.Tonality))
                {
                    var autoTonality = MidiKeySignatureDetector.TryDetectTonality(midifilePath);
                    if (!string.IsNullOrWhiteSpace(autoTonality))
                    {
                        Melody.Tonality = autoTonality; // якщо у вас є Tonality4, замініть на MusicMelody.Tonality
                    }
                }

                // перевірка на поліфонію 
                var ifeligible = IfMonody(midifilePath);

                // створює mp3 на основі MIDI та завантажує на сервер якщо не поліфонічний (існуючий перезаписує)
                if (ifeligible)
                {
                    try
                    {
                        await PrepareMp3Async(_environment, midifilePath, false);
                        Melody.IsFileEligible = true;
                        ViewData["Message"] = "Файл успішно завантажено!";
                    }
                    catch
                    {
                        ViewData["Message"] = "Не вдалося завантажити файл";
                        _logger.LogWarning("Failed to load file");
                    }
                }
                else
                {
                    ViewData["Message"] = "Файл не є мелодією. Перевірте.";
                    Melody.IsFileEligible = false;
                }

                // СПОВІЩЕННЯ НА ТЕЛЕГРАМ 
                await NotifyTelegram(savedFileName);
            }

            // ЯКЩО ФАЙЛ СТВОРЕНО НА ВІРТУАЛЬНІЙ КЛАВІАТУРІ САЙТУ
            else if (!string.IsNullOrEmpty(TempMidiFilePath))
            {

                string newfilename = $"{Translit.Transliterate(Melody.Author.Surname)}_{Translit.Transliterate(Melody.Title)}.mid";

                // destination depends on role
                var destDir = isPrivileged ? publishedDir : incomingDir;
                var midifilePath = Path.Combine(destDir, newfilename);

                // ensure unique filename
                var baseName = Path.GetFileNameWithoutExtension(newfilename);
                var ext = Path.GetExtension(newfilename);
                int i = 0;
                while (System.IO.File.Exists(midifilePath))
                {
                    i++;
                    var candidate = $"{baseName}_{i}{ext}";
                    midifilePath = Path.Combine(destDir, candidate);
                    newfilename = Path.GetFileName(midifilePath);
                }

                MessageL(COLORS.green, $"try to move file {TempMidiFilePath} to {midifilePath}");
                try
                {
                    System.IO.File.Move(TempMidiFilePath, midifilePath);
                    await PrepareMp3Async(_environment, midifilePath, false);
                    Melody.IsFileEligible = true;
                    ViewData["Message"] = "Файл успішно завантажено!";
                    Melody.FilePath = Path.GetFileName(midifilePath);
                    GrayMessageL($"файл завантажено!");

                    savedMidifilePath = midifilePath;
                    savedFileName = Path.GetFileName(midifilePath);

                    // СПОВІЩЕННЯ НА ТЕЛЕГРАМ 
                    await NotifyTelegram(savedFileName);

                }
                catch (IOException ex)
                {
                    _logger?.LogError($"Помилка переміщення файлу: {ex.Message}");                    
                    TempData["ErrorMessage"] = "Помилка переміщення файлу";
                }
            }
            else
            {
                ErrorMessageL("fileupload is null");
            }


            MessageL(COLORS.cyan, "OnPostAsync finished (saved to DB)");

            // ЗБЕРЕЖЕННЯ У БД ДЛЯ АДМІНІВ ТА МОДЕРАТОРІВ
            if (isPrivileged)
            {
                // ensure FilePath contains only filename
                if (!string.IsNullOrEmpty(savedFileName))
                    Melody.FilePath = savedFileName;

                _context.Melody.Add(Melody);
                await _context.SaveChangesAsync();
                var recentmelody = await _context.Melody.FirstOrDefaultAsync(m => m.Title == Melody.Title && m.Author == Melody.Author);

                MessageL(COLORS.cyan, "OnPostAsync finished (saved to DB)");

                                
                return RedirectToPage("./Details", new { id = recentmelody?.ID });
            }
            // ЗБЕРЕЖЕННЯ ЛИШЕ JSON ДЛЯ ПЕРЕВІРКИ МОДЕРАТОРОМ
            else
            {
                
                MessageL(COLORS.cyan, "OnPostAsync finished (saved as JSON only)");
                try
                {
                    var addedBy = User?.Identity?.Name ?? "anonymous";
                    await SaveMelodyJsonAsync(Melody, addedBy);
                    TempData["Message"] = "Мелодію збережено для перевірки (JSON). Очікуйте модерації.";
                    return RedirectToPage("./Create");
                }
                catch (Exception ex)
                {
                    ErrorMessageL($"Failed to save melody JSON: {ex.Message}");
                    TempData["ErrorWarning"] = "Не вдалося зберегти заявку. Спробуйте пізніше.";
                }

                // Return to Create page so user can see message
                GetAuthorsData();
                GetTonalitiesData();
                return Page();
            }
        }

        // Збереження JSON file wwwroot/json/melodies для перевірки модератором
        private async Task SaveMelodyJsonAsync(Melody melody, string addedBy)
        {
            if (melody == null) return;

            var midifileDir = Path.Combine(_environment.WebRootPath, "melodies"); 
            if (!Directory.Exists(midifileDir)) Directory.CreateDirectory(midifileDir);
            var incomingDir = Path.Combine(_environment.WebRootPath, "melodies", "incoming");
            if (!Directory.Exists(incomingDir)) Directory.CreateDirectory(incomingDir);

            // Attempt to load music.MusicMelody from saved midi file if available (now in incoming)
            List<string>? notes = null;
            List<int>? intervals = null;
            List<int>? pitches = null;
            try
            {
                if (!string.IsNullOrEmpty(melody.FilePath))
                {
                    var midipath = Path.Combine(midifileDir, melody.FilePath);
                    if (System.IO.File.Exists(midipath))
                    {
                        var mf = GetMidiFile(midipath);
                        var musicMel = await GetMelodyFromMidiAsync(mf);
                        if (musicMel is not null)
                        {
                            notes = musicMel.NotesList;
                            intervals = musicMel.IntervalList;
                            pitches = musicMel.PitchesList;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // ignore parse errors, still save basic metadata
                _logger?.LogError($"Failed to extract MIDI details for JSON: {e.Message}");
            }

            var dto = new
            {
                Id = melody.ID,
                Title = melody.Title,
                Author = melody.Author is not null ? new { melody.Author.Name, melody.Author.Surname } : null,
                FilePath = melody.FilePath,
                Tonality = melody.Tonality,
                Tempo = melody.MidiMelody?.Tempo ?? 0,
                Description = melody.Description,
                AddedBy = addedBy,
                AddedAt = DateTime.UtcNow.ToString("o")
            };

            var opts = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(dto, opts);

            var safeTitle = Translit.Transliterate(melody.Title ?? "untitled");
            var safeAuthor = Translit.Transliterate(melody.Author?.Surname ?? "unknown");
            var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{safeAuthor}_{safeTitle}_{melody.ID}.json";
            var filePath = Path.Combine(incomingDir, fileName);

            await System.IO.File.WriteAllTextAsync(filePath, json);
            _logger?.LogInformation($"Saved melody JSON: {filePath}");
            
        }

        private static async Task NotifyTelegram(string newfilename)
        {
            // СПОВІЩЕННЯ НА ТЕЛЕГРАМ 
            try
            {
                var telegramService = new TelegramService();
                await telegramService.SendNotificationAsync($"{DateTime.Now} - на нашому сайті оновлення: завантажено файл {newfilename}");
            }
            catch (Exception e)
            {
                ErrorMessageL("Неможливо сповістити телеграм");
                GrayMessageL(e.Message);
            }
        }

        public async Task<IActionResult> OnPostAddform()
        {
            Console.WriteLine($"Adding author form - {TempAuthor}");
            ShowAuthor = true;

            if (!string.IsNullOrEmpty(TempAuthor))
            {
                if (TempAuthor.Contains(",.;?!"))
                {
                    TempData["ErrorWarning"] = "Некоректно введено автора";
                    return Page();
                }

                string tempName = "", tempSurname = "";

                var author = (TempAuthor.Split(" "));
                if (author.Length == 1)
                    tempSurname = author[0];
                else if (author.Length == 2)
                {
                    tempName = author[0];
                    tempSurname = author[1];
                }
                else if (author.Length == 3)
                {
                    tempName = author[0] + " " + author[1];
                    tempSurname = author[2];
                }
                else
                {
                    TempData["ErrorWarning"] = "Некоректне введено автора";
                    return Page();
                }

                var newAuthor = new Author { Name = tempName, Surname = tempSurname };

                _context.Author.Add(newAuthor);

                /* Збереження змін до бази даних */

                await _context.SaveChangesAsync();

                /* Пошук доданого автора для отримання його ID */

                var tryaddauthor = await _context.Author.FirstOrDefaultAsync(a => a.Surname == tempSurname && a.Name == tempName);



                if (tryaddauthor is not null)
                {
                    Console.WriteLine($"trying to pass value {tryaddauthor.ID}");
                    return RedirectToPage("Create", new { selectedAuthorId = tryaddauthor.ID });

                }
                else
                {
                    TempData["ErrorWarning"] = $"Не вдалося додати автора {TempAuthor}";
                    return Page();
                }

            }

            else return Page();
        }


        public async Task<IActionResult> OnGetCheckTitleAsync(string title)
        {
            bool exists = await _context.Melody.AnyAsync(m => m.Title == title);
            Console.WriteLine($"Checking for title");
            return new JsonResult(new { exists });
        }

        public async Task<IActionResult> OnGetCheckAuthorAsync(string author)
        {
            Console.WriteLine($"Checking for author {author}");
            bool exists = await _context.Author.AnyAsync(m => author.Contains(m.Surname) || author.Contains(m.SurnameEn));
            return new JsonResult(new { exists });
        }

        public async Task<JsonResult> OnGetCheckAuthorFullAsync(string name, string surname)
        {
            Console.WriteLine($"Checking for full author match: name={name}, surname={surname}");
            bool exists = await _context.Author.AnyAsync(a => a.Name == name && a.Surname == surname);
            return new JsonResult(new { exists });
        }


        public IActionResult OnPostPiano(string key)
        {
            if (TempData["ErrorWarning"] is not null)
            {
                ErrorWarning = TempData["ErrorWarning"] as string;
            }

            Globals.notation = Notation.eu;

            Console.WriteLine($"Key pressed: {key}");

            if (!string.IsNullOrEmpty(key))
            {
                MessageL(COLORS.yellow, $"CREATE - OnPostPiano method {key}");
                Keys += key + " ";
            }
            else
            {
                MessageL(COLORS.yellow, $"CREATE - OnPostPiano method, no key, return");
                TempData["ErrorWarning"] = "";
                return Page();
            }
            var note = new Note(key);

            // відтворення ноти
            try
            {
                string mp3Path = Path.Combine(_environment.WebRootPath, "sounds", $"{key}.mp3");
                if (!System.IO.File.Exists(mp3Path))
                    GenerateMp3(note, mp3Path);
                else Console.WriteLine("using existing file");
                var relativePath = "/sounds/" + Path.GetFileName(mp3Path);
                TempData["AudioFile"] = relativePath;
                Console.WriteLine($"playing mp3 {mp3Path}");
            }
            catch (Exception ex)
            {
                Msg = ex.ToString();
                TempData["ErrorWarning"] = ex.ToString();
            }


            return Page();
        }


        // для createMelody.js
        public async Task<JsonResult> OnGetCheckItemExistsAsync(string title, int authorId)
        {
            bool exists = await _context.Melody
                .AnyAsync(m => m.Title == title && m.AuthorID == authorId);

            return new JsonResult(exists);
        }

        // GET: /Melodies/Create?handler=AuthorSearch&q=...
        public async Task<IActionResult> OnGetAuthorSearchAsync(string q)
        {
            q = (q ?? string.Empty).Trim();
            if (q.Length < 3)
                return new JsonResult(Array.Empty<object>());

            // Case-insensitive contains across four fields
            string like = $"%{q}%";

            var authors = await _context.Author
                .Where(a =>
                    EF.Functions.Like(a.Name ?? string.Empty, like) ||
                    EF.Functions.Like(a.Surname ?? string.Empty, like) ||
                    EF.Functions.Like(a.NameEn ?? string.Empty, like) ||
                    EF.Functions.Like(a.SurnameEn ?? string.Empty, like))
                .OrderBy(a => a.Surname).ThenBy(a => a.Name)
                .Take(20)
                .Select(a => new
                {
                    id = a.ID,                 // use ID from your model
                    name = a.Name,
                    surname = a.Surname,
                    nameEn = a.NameEn,
                    surnameEn = a.SurnameEn,
                    displayName = ((a.Surname ?? "") + " " + (a.Name ?? "")).Trim()
                })
                .ToListAsync();

            return new JsonResult(authors);
        }
    }
}









