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
using NAudio.Midi;
using Music;
using static Music.Messages;
using Microsoft.EntityFrameworkCore;

namespace Melodies25.Pages.Melodies
{
    public class CreateModel : PageModel
    {
        private readonly Melodies25.Data.Melodies25Context _context;

        private readonly IWebHostEnvironment _environment;

        [BindProperty]
        public Models.Melody Melody { get; set; } = default!;


        public CreateModel(Melodies25.Data.Melodies25Context context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public IActionResult OnGet()
        {
            ViewData["AuthorID"] = new SelectList(_context.Author, "ID", "Surname");
            return Page();
        }


        public async Task<IActionResult> OnPostAsync(IFormFile? fileupload)
        {
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
                Console.WriteLine("Author not found!");
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

                var filePath = Path.Combine(uploadsPath, newfilename);

                // Записуємо файл на сервер
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await fileupload.CopyToAsync(stream);
                }


                try
                {
                    var midiFile = new MidiFile(filePath);

                    var ispoliphonic = MidiConverter.CheckForPolyphony(midiFile);


                    if (ispoliphonic)
                        MessageL(COLORS.yellow, "Виявлено поліфонію!");
                    else
                        MessageL(COLORS.blue, "Одноголосний!");
                }
                catch (Exception ex)
                {
                    ErrorMessage($"failed to check file: {ex}");
                }


                ViewData["Message"] = "Файл успішно завантажено!";
                
                Melody.Filepath = newfilename; //назву файлу фіксуємо
                              

            }
            else
            {
                Console.WriteLine("fileupload is null");
            }        


            _context.Melody.Add(Melody);
            await _context.SaveChangesAsync();

            


            return RedirectToPage("./Index");
        }

        
    }

}

