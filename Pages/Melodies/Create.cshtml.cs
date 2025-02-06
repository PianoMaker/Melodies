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

namespace Melodies25.Pages.Melodies
{
    public class CreateModel : PageModel
    {
        private readonly Melodies25.Data.Melodies25Context _context;

        private readonly IWebHostEnvironment _environment;

        [BindProperty]
        public Melody Melody { get; set; } = default!;


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
                string newfilename = Translit.Transliterate(fileupload.FileName);

                var filePath = Path.Combine(uploadsPath, newfilename);

                // Записуємо файл на сервер
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await fileupload.CopyToAsync(stream);
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

