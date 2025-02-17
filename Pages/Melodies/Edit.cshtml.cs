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
using static Melodies25.Utilities.PrepareFiles;
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
            MessageL(COLORS.yellow, "MELODIES/EDIT OnGet");

            if (id == null)
            {
                return NotFound();
            }

            var melody = await _context.Melody.Include(m => m.Author).
                FirstOrDefaultAsync(m => m.ID == id);
            if (melody == null)
            {
                return NotFound();
            }
            Melody = melody;

            ViewData["AuthorID"] = new SelectList(_context.Author, "ID", "Surname");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(IFormFile? fileupload)
        {
            MessageL(COLORS.yellow, "MELODIES/EDIT OnPost");
            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    ErrorMessage(error.ErrorMessage);
                }
            }

            var uploadsPath = Path.Combine(_environment.WebRootPath, "melodies");

            //завантаження файлу якщо є
            if (fileupload is not null)
            {
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
                //підвантажуємо імена авторів
                Melody.Author = await _context.Author
    .FirstOrDefaultAsync(a => a.ID == Melody.AuthorID);

                string newfilename;

                if (Melody.Author is not null)
                {
                    newfilename = $"{Translit.Transliterate(Melody.Author.Surname)}_{Translit.Transliterate(Melody.Title)}.mid";

                }
                else
                {
                    newfilename = $"{Translit.Transliterate(Melody.Title)}.mid";
                }
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
                Console.WriteLine("fileupload is null or author is null");
            }

            _context.Attach(Melody).State = EntityState.Modified;

            //перевірка на поліфоню
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


            _context.Entry(Melody).Property(m => m.Title).IsModified = true;
            _context.Entry(Melody).Property(m => m.Year).IsModified = true;
            _context.Entry(Melody).Property(m => m.AuthorID).IsModified = true;
            _context.Entry(Melody).Property(m => m.Description).IsModified = true;

            if (fileupload is not null)
            {
                _context.Entry(Melody).Property(m => m.Filepath).IsModified = true;
            }
            else //якщо не перезавантажено файл, зміни в це поле БД не вносяться
                _context.Entry(Melody).Property(m => m.Filepath).IsModified = false;


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

            return RedirectToPage("./Index");

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

            if (melody.Filepath is not null)
            {
                await PrepareMp3Async(_environment, melody.Filepath, false);
            }

            return RedirectToPage("Details", new { id = melody.ID });
        }
    }
}
