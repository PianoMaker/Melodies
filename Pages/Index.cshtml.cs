using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using static Music.Messages;

namespace Melodies25.Pages
{
    public class IndexModel : PageModel
    {
        [BindProperty]
        public bool Singerella { get; set; }

        [BindProperty]
        public string? Search { get; set; }

        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            MessageL(14, "Index OnGet");
        }

        public IActionResult OnPostSearch()
        {
            MessageL(14, "Index OnPostSearch");

            return RedirectToPage("./Melodies/Search", new { search = Search });
        }

        public IActionResult OnPostAdvancedSearch()
        {
            MessageL(14, "Index OnPostAdvancedSearch");
            return RedirectToPage("./Melodies/Search", new { search = Search });
        }

        public IActionResult OnPostChangeDesign()
        {
            MessageL(14, "Index OnPostChangeDesign");
            return Page();
        }
    }
}
