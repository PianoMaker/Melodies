using Melodies25.Models;
using Melodies25.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;


namespace Melodies25.Pages.Admin
{
    public class SettingsModel : PageModel
    {
        [BindProperty]
        public LoggingSettings Logging { get; set; }

        public void OnGet()
        {
            // read current cached settings
            Logging = FileSettingsProvider.Logging;
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid) return Page();

            FileSettingsProvider.Save(Logging);
            TempData["Saved"] = "Settings saved";
            return RedirectToPage();
        }
    }
}