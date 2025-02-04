using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Melodies25.Pages.Melodies
{
    public class SearchModel : PageModel
    {

        [BindProperty]
        public string Note { get; set; }
        public void OnGet()
        {

        }
    }
}
