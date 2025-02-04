using Melodies25.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Melodies25.Pages.Melodies
{
    public class SearchModel : PageModel
    {
        private readonly Melodies25.Data.Melodies25Context _context;

        public IList<Melody> Melody { get; set; } = default!;

        public SearchModel(Melodies25.Data.Melodies25Context context)
        {
            _context = context;
        }


        [BindProperty]
        public string Note { get; set; }

        [BindProperty]
        public string Author { get; set; }

        [BindProperty]
        public string Title { get; set; }

        [BindProperty]
        public bool IfPartly { get; set; }


        public async Task OnGetAsync()
        {
            Page();
        }

        public async Task OnPostAsync()
        {
            // Очищення попередніх результатів
            Melody = new List<Melody>();

            IQueryable<Melody> query = _context.Melody.Include(m => m.Author);

            if (IfPartly == false)
            {
                if (!string.IsNullOrWhiteSpace(Author))
                {
                    string authorLower = Author.ToLower();
                    query = query.Where(m => m.Author.Surname.ToLower() == authorLower);
                }

                if (!string.IsNullOrWhiteSpace(Title))
                {
                    string titleLower = Title.ToLower();
                    query = query.Where(m => m.Title.ToLower() == titleLower);
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(Author))
                {
                    string authorLower = Author.ToLower();
                    query = query.Where(m => m.Author.Surname.ToLower().Contains(authorLower));
                }

                if (!string.IsNullOrWhiteSpace(Title))
                {
                    string titleLower = Title.ToLower();
                    query = query.Where(m => m.Title.ToLower().Contains(titleLower));
                }
            }

            Melody = await query.ToListAsync();
        }

    }
}
