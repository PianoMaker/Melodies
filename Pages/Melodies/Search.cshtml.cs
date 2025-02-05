using Melodies25.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Music;
using static Music.Messages;
using Melody = Melodies25.Models.Melody;

namespace Melodies25.Pages.Melodies
{
    public class SearchModel : PageModel
    {
        private readonly Melodies25.Data.Melodies25Context _context;
        private readonly IWebHostEnvironment _environment;

        public IList<Melody> Melody { get; set; } = default!;

        public SearchModel(Melodies25.Data.Melodies25Context context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }


        [BindProperty]
        public string Note { get; set; }

        [BindProperty]
        public string Author { get; set; }

        [BindProperty]
        public string Title { get; set; }

        [BindProperty]
        public bool IfPartly { get; set; }


        public void OnGetAsync()
        {
                       
            Page();
        }

        private async Task NotesSearchInitialize()
        {
            Melody = _context.Melody.ToList();

            foreach (var melody in Melody)
            {
                var wwwRootPath = Path.Combine(_environment.WebRootPath, "melodies");

                if (melody.Filepath is not null)
                {
                    var path = Path.Combine(wwwRootPath, melody.Filepath);

                    Console.WriteLine($"{melody.Title} exploring file {path}");

                    var midifile = MidiConverter.GetMidiFile(path);

                    melody.MidiMelody = MidiConverter.GetMelodyFromMidi(midifile);
                }
                else
                {
                    MessageL(COLORS.yellow, $"{melody.Title} has no file path");
                }

            }
        }

        public async Task OnPostAsync()
        {
            await NotesSearchInitialize();
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
                        
            if (!string.IsNullOrWhiteSpace(Note))
            {
                var firstnote = new Note(Note);
                var filteredMelodies = new List<Melody>();
                
                
                foreach (var melody in Melody)
                {
                    MessageL(COLORS.green, $"checking melody {melody.Title}");
                                        
                    if (melody.MidiMelody is null)
                    { 
                        ErrorMessage("no midi found\n");                     
                    }
                    else if (melody.MidiMelody.IfStartsFromNote(firstnote))
                    {
                        filteredMelodies.Add(melody); // Додаємо в список результатів
                        Message(COLORS.blue, "+");
                    }
                    else
                    {
                        Message(COLORS.blue, "-");
                    }
                }

                Melody = filteredMelodies;
            }

            
        }




    }
}
