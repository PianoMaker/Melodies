using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Melodies25.Data;
using Melodies25.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.IO;

namespace Melodies25.Pages.Melodies
{
    //[Authorize(Roles = "Admin")] - це повне вимкнення
    public class DeleteModel : PageModel
    {
        private readonly Melodies25.Data.Melodies25Context _context;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly IWebHostEnvironment _environment;
        public DeleteModel(Melodies25.Data.Melodies25Context context, UserManager<IdentityUser> userManager, IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        [BindProperty]
        public Melody Melody { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            /*м'яке посилання користувача */
            var user = await _userManager.GetUserAsync(User);
            if (user is not null)
            {
                var isAdminOrModerator = await _userManager.IsInRoleAsync(user, "Admin") || await _userManager.IsInRoleAsync(user, "Moderator");
                if (!isAdminOrModerator)
                {
                    return RedirectToPage("/Shared/AccessDenied");
                }
            } 
            else return RedirectToPage("/Shared/AccessDenied");
            /**/

                var melody = await _context.Melody.FirstOrDefaultAsync(m => m.ID == id);

            if (melody == null)
            {
                return NotFound();
            }
            else
            {
                Melody = melody;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var melody = await _context.Melody.FindAsync(id);
            if (melody != null)
            {
                Melody = melody;
                if(Melody.Filepath is not null)
                {
                    var filename = Melody.Filepath;
                    var uploadsPath = Path.Combine(_environment.WebRootPath, "melodies");
                    var filePath = Path.Combine(uploadsPath, filename);
                    try
                    {
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                            Console.WriteLine($"Файл {filename} успішно видалено.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Помилка при видаленні файлу: {ex.Message}");
                    }
                }

                _context.Melody.Remove(Melody);
                await _context.SaveChangesAsync();

                

            }

            return RedirectToPage("./Index");
        }
    }
}
