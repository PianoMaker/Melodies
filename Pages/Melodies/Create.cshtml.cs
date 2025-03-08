using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Melodies25.Data;
using Melodies25.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Routing.Constraints;
using System.Text;
using Melodies25.Utilities;
using static Melodies25.Utilities.Algorythm;
using static Melodies25.Utilities.SynthWaveProvider;
using NAudio.Midi;
using Music;
using static Music.Messages;
using static Music.MidiConverter;
using static Melodies25.Utilities.PrepareFiles;
using Microsoft.EntityFrameworkCore;
using Melody = Melodies25.Models.Melody;

namespace Melodies25.Pages.Melodies
{
    public class CreateModel : PageModel
    {
        private readonly Melodies25.Data.Melodies25Context _context;

        private readonly IWebHostEnvironment _environment;

        public int? SelectedAuthorID { get; set; }

        [BindProperty]
        public bool ShowAuthor { get; set; }

        [BindProperty]
        public string TempAuthor { get; set; }

        [BindProperty]
        public Models.Melody Melody { get; set; } = default!;

        [BindProperty]
        public string? SelectedMode { get; set; }

        public string? ErrorWarning { get; set; }

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
            ViewData["AuthorID"] = new SelectList(_context.Author.OrderBy(a => a.Surname), "ID", "Surname", Melody?.AuthorID);
            ViewData["Tonalities"] = new SelectList(new List<string>
            {
                "C-dur", "G-dur", "D-dur", "A-dur", "E-dur", "H-dur", "Fis-dur", "Cis-dur",
                "F-dur", "B-dur", "Es-dur", "As-dur", "Des-dur", "Ges-dur", "Ces-dur",
                "a-moll", "e-moll", "h-moll", "fis-moll", "cis-moll", "gis-moll", "dis-moll", "ais-moll",
                "d-moll", "g-moll", "c-moll", "f-moll", "b-moll", "es-moll", "as-moll"
            });

            ShowAuthor = false;
            return Page();
        }


        public async Task<IActionResult> OnPostAsync(IFormFile? fileupload)
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

                var telegramService = new TelegramService();
                await telegramService.SendNotificationAsync($"{DateTime.Now} - на нашому сайті оновлення: завантажено файл {newfilename}");

            }
            else
            {
                Console.WriteLine("fileupload is null");
            }


            _context.Melody.Add(Melody);
            await _context.SaveChangesAsync();
            var recentmelody = await _context.Melody.FirstOrDefaultAsync(m => m.Title == Melody.Title && m.Author == Melody.Author);

            return RedirectToPage("./Details", new { id = recentmelody?.ID});
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


    }

}

