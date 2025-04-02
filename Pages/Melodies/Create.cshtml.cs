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

namespace Melodies25.Pages.Melodies
{
    public class CreateModel : PageModel
    {
        private readonly Melodies25.Data.Melodies25Context _context;

        private readonly IWebHostEnvironment _environment;

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

        [BindProperty]
        public string Keys { get; set; } = default!;

        public Music.Melody NewPattern { get; set; }
        internal string TempFilePath { get; set; }

        private static readonly char[] separator = new char[] { ' ', '_' };

        public CreateModel(Melodies25.Data.Melodies25Context context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public IActionResult OnGet(int selectedAuthorId)
        {

            if (Melody == null)
                Melody = new Melody();

            if (selectedAuthorId > 0)
                Melody.AuthorID = selectedAuthorId;


            MessageL(COLORS.yellow, "MELODIES/CREATE OnGet");
            GetAuthorsData();
            GetTonalitiesData();

            ShowAuthor = false;
            return Page();
        }

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
        public void OnPostMelody()
        {
            MessageL(COLORS.yellow, $"MELODIES/CREATE - OnPostMelody method, keys = {Keys}");            
            GetAuthorsData();
            GetTonalitiesData();            

            if (Keys is not null)
            {
                /* Будуємо послідовність введених нот */
                Music.Melody MelodyPattern = new();
                Globals.notation = Notation.eu;
                Globals.lng = LNG.uk;
                BuildPattern(MelodyPattern);
                NewPattern = (Music.Melody)MelodyPattern.Clone();
                try
                {                    
                    string filename = "userFile" + DateTime.Now.ToShortDateString() + DateTime.Now.Hour + DateTime.Now.Minute + DateTime.Now.Second;
                    var tempUploads = Path.Combine(_environment.WebRootPath, "temporary");
                    
                    if (!Directory.Exists(tempUploads))
                        Directory.CreateDirectory(tempUploads);
                    TempFilePath = Path.Combine(tempUploads, filename) + ".mid";
                    MelodyPattern.SaveMidi(TempFilePath);
                    MessageL(COLORS.green, "file is ready");
                    Msg = $"file is ready, path = {TempFilePath}";
                    TempData["TempFilePath"] = TempFilePath;
                    TempData["Keys"] = Keys;
                    MessageL(COLORS.gray, $"Keys = {TempData["Keys"]}");
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"Проблема з доступом до файлу: {ex.Message}");
                }
                catch (Exception e)
                {
                    ErrorMessageL(e.ToString());
                    Msg = "error occured";
                    Console.WriteLine($"Проблема: {e.Message}");
                }
            }
            else
            {
                ErrorMessageL("keys are null");
            }
            MessageL(COLORS.gray, "OnPostMelody is finished");
            RedirectToPage();
        }


        private void BuildPattern(Music.Melody MelodyPattern)
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
                    ErrorMessage($"impossible to read note {key}\n");
                }
            }
        }

        public void OnPostAsync(string key)
        {
            //подолання глюку
            MessageL(COLORS.yellow, $"MELODIES/CREATE - OnPostAsync method {key}");
            GetAuthorsData();
            GetTonalitiesData();
            OnPostPiano(key);
        }

        /* ЗАПИС НОВОЇ МЕЛОДІЇ НА СЕРВЕР */
        public async Task<IActionResult> OnPostCreateAsync(IFormFile? fileupload)
        {

            MessageL(COLORS.yellow, "MELODIES/CREATE OnPostAsync");


            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine(error.ErrorMessage);
                }
                //return Page();
            }

            TempFilePath = TempData["TempFilePath"] as string??"";
            

            Melody.Author = await _context.Author
    .FirstOrDefaultAsync(a => a.ID == Melody.AuthorID);

            if (Melody.Author == null)
            {
                Console.WriteLine("Author has not been not found!");
                return Page(); // або обробити помилку іншим чином
            }


            var uploadsPath = Path.Combine(_environment.WebRootPath, "melodies");
            


            // Створюємо папку, якщо її немає
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
                Console.WriteLine($"{uploadsPath} created");
            }
            else
            {
                Console.WriteLine($"{uploadsPath} exists");
            }

            
            //завантаження файлу якщо є
            if (fileupload is not null)
            {
                string newfilename = $"{Translit.Transliterate(Melody.Author.Surname)}_{Translit.Transliterate(Melody.Title)}.mid";
                var midifilePath = Path.Combine(uploadsPath, newfilename);

                MessageL(COLORS.green, $"try to process uploaded file {newfilename}");
                // Записуємо файл на сервер
                using (var stream = new FileStream(midifilePath, FileMode.Create))
                {
                    await fileupload.CopyToAsync(stream);
                }

                // перевірка на поліфоню 
                var ifeligible = IfMonody(midifilePath);

                // завантажує mp3 на сервер якщо не поліфонічний (існуючий перезаписує)
                if (ifeligible)
                {
                    await PrepareMp3Async(_environment, midifilePath, false);
                    Melody.IsFileEligible = true;
                    ViewData["Message"] = "Файл успішно завантажено!";
                }
                else
                {
                    ViewData["Message"] = "Файл не є мелодією. Перевірте.";
                    Melody.IsFileEligible = false;
                }

                Melody.Filepath = newfilename; //назву файлу фіксуємо

                // СПОІВЩЕННЯ НА ТЕЛЕГРАМ 

                await NotifyTelegram(newfilename);

            }
            
            // запис новоствореного файлу
            else if (!string.IsNullOrEmpty(TempFilePath))
            {
                
                string newfilename = $"{Translit.Transliterate(Melody.Author.Surname)}_{Translit.Transliterate(Melody.Title)}.mid";
                var midifilePath = Path.Combine(uploadsPath, newfilename);
                MessageL(COLORS.green, $"try to move file starts to {newfilename}");
                try
                {
                    System.IO.File.Move(TempFilePath, midifilePath);
                    await PrepareMp3Async(_environment, midifilePath, false);
                    Melody.IsFileEligible = true;
                    ViewData["Message"] = "Файл успішно завантажено!";
                    Melody.Filepath = newfilename;

                    await NotifyTelegram(newfilename);

                }
                catch (IOException ex)
                {
                    
                    ErrorMessage($"Помилка переміщення файлу: ");
                    GrayMessageL($"{ex.Message}");
                }

            }

            else
            {
                ErrorMessageL("fileupload is null");
            }


            _context.Melody.Add(Melody);
            await _context.SaveChangesAsync();
            var recentmelody = await _context.Melody.FirstOrDefaultAsync(m => m.Title == Melody.Title && m.Author == Melody.Author);

            return RedirectToPage("./Details", new { id = recentmelody?.ID});
        }

        private static async Task NotifyTelegram(string newfilename)
        {

            // СПОІВЩЕННЯ НА ТЕЛЕГРАМ 

            var telegramService = new TelegramService();
            await telegramService.SendNotificationAsync($"{DateTime.Now} - на нашому сайті оновлення: завантажено файл {newfilename}");
        }

        public async Task<IActionResult> OnPostAddform()
        {
            Console.WriteLine($"Adding author form - {TempAuthor}");
            ShowAuthor = true;

            if (!string.IsNullOrEmpty(TempAuthor))
            {
                if (TempAuthor.Contains(",.;?!"))
                {
                    ErrorWarning = "Некоректно введено автора";
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
                    ErrorWarning = "Некоректне введено автора";
                    return Page(); 
                }

                var newAuthor = new Author { Name = tempName, Surname = tempSurname };

                _context.Author.Add(newAuthor);

                await _context.SaveChangesAsync();

                var tryaddauthor = await _context.Author.FirstOrDefaultAsync(a => a.Surname == tempSurname && a.Name == tempName);

                Console.WriteLine("current title is " + Melody.Title);

                if (tryaddauthor is not null)
                {
                    Console.WriteLine($"trying to pass value {tryaddauthor.ID}");
                    return RedirectToPage("Create", new { selectedAuthorId = tryaddauthor.ID });

                }
                else
                {
                    ErrorWarning = $"Не вдалося додати автора {TempAuthor}";
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


        public IActionResult OnPostPiano(string key)
        {
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
            }
           

            return Page();
        }

        
    }

}

