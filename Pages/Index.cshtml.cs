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
        public string Search { get; set; }



        public void OnGet()
        {
            Console.WriteLine("Index OnGet");
        }

        public IActionResult OnPost()
        {
            Console.WriteLine("Index OnPost");

            return RedirectToPage("./Melodies/Search", new { search = Search });
        }
    }
}
