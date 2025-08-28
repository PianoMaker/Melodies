using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Melody = Melodies25.Models.Melody;

namespace Melodies25.Pages
{
    public class IndexModel : PageModel
    {


        [BindProperty]
        public bool Singerella { get; set; }

        [BindProperty]
        public string? Search { get; set; }



        public void OnGet()
        {
            Console.WriteLine("Index OnGet");
        }

        public IActionResult OnPostSearch()
        {
            Console.WriteLine("Index OnPostSearch");

            return RedirectToPage("./Melodies/Search", new { search = Search });
        }

        public IActionResult OnPostAdvancedSearch()
        {
            Console.WriteLine("OnPostAdvancedSearch");
            return RedirectToPage("./Melodies/Search", new { search = Search });
        }


        public IActionResult OnPostChangeDesign()
        {
            Console.WriteLine("OnPostChangeDesign");
            return Page();
        }
    }
}
