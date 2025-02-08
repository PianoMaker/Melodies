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
using Melody = Melodies25.Models.Melody;

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

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var melody =  await _context.Melody.FirstOrDefaultAsync(m => m.ID == id);
            if (melody == null)
            {
                return NotFound();
            }
            Melody = melody;
           ViewData["AuthorID"] = new SelectList(_context.Author, "ID", "Surname");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(IFormFile? fileupload, int? id)
        {

            var melody = await _context.Melody
                .Include(m => m.Author)
                .FirstOrDefaultAsync(m => m.ID == id);
                
                        
            if(melody is null) return NotFound();
            else Melody = melody;

            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine(error.ErrorMessage);
                }
                //return Page();
                //тестовий режим
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
                var newfilename = $"{Translit.Transliterate(Melody.Author.Surname)}_{Translit.Transliterate(Melody.Title)}.mid";
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

            _context.Attach(Melody).State = EntityState.Modified;


            if (Melody.Filepath is not null)
            {
                try
                {
                    var filePath = Path.Combine(_environment.WebRootPath, "melodies", Melody.Filepath);

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
            }


            try
            {
                await _context.SaveChangesAsync();
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

            return RedirectToPage("./Index");
        }

        
        public async Task<IActionResult> OnPostDeleteFileAsync()
        {
            var melody = await _context.Melody.FindAsync(Melody.ID);
            if (melody == null)
            {
                return NotFound();
            }

            /*м'яке посилання користувача */
            var user = await _userManager.GetUserAsync(User);
            if (user is not null)
            {
                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                if (!isAdmin)
                {
                    return RedirectToPage("/Shared/AccessDenied");
                }
            }
            else return RedirectToPage("/Shared/AccessDenied");
            /**/

            if (!string.IsNullOrEmpty(melody.Filepath))
            {
                string filePath = Path.Combine(_environment.WebRootPath, "uploads", melody.Filepath);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                melody.Filepath = null; // Очистити шлях у БД
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
