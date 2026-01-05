using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Melodies25.Pages.Melodies
{
    public class PreviewModel : PageModel
    {
        public string? FilePath { get; set; }

        public string? FileName { get; set; }

        public void OnGet(string? path, string? name)
        {
            FilePath = path;
            FileName = name;
            Console.WriteLine($"PreviewModel OnGet called with path: {FilePath}, name: {FileName}");          
        }
    }
}
