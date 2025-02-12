using Melodies25.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Music;
using Newtonsoft.Json;
using System.Globalization;
using System.IO;
using static Melodies25.Utilities.Algorythm;
using static Melodies25.Utilities.SynthWaveProvider;
using static Music.Messages;
using Melody = Melodies25.Models.Melody;

namespace Melodies25.Pages.Melodies
{
    public class SearchModel : PageModel
    {
        private readonly Melodies25.Data.Melodies25Context _context;
        private readonly IWebHostEnvironment _environment;

        public IList<Melody> Melody { get; set; } = default!;

        public List<(Melody melody, int commonLength)> MatchedMelodies { get; set; } = new();

        public bool NoteSearch { get; set; }
        public string Msg { get; set; }

        public string Errormsg { get; set; }

        public SearchModel(Melodies25.Data.Melodies25Context context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        private int minimummatch = 2;//вивести на контроллер

        [BindProperty]
        public string Note { get; set; }

        [BindProperty]
        public string Author { get; set; }

        [BindProperty]
        public string Title { get; set; }

        [BindProperty]
        public bool IfPartly { get; set; }

        [TempData]
        public string Description { get; set; }

        [TempData]
        public string Keys { get; set; }



        public void OnGetAsync(string search)
        {
            

            if (!string.IsNullOrEmpty(search))
            {
                Console.WriteLine($"Received search: {search}");

                Melody = new List<Melody>();

                string searchQuery = search.ToLower();

                var query = _context.Melody.AsQueryable();

                var results = query.Where(m => EF.Functions.Like(m.Author.Surname, $"%{searchQuery}%"))
                                   .Concat(query.Where(m => EF.Functions.Like(m.Title, $"%{searchQuery}%")));

                Melody = results.ToList();

                if (Melody.Count == 0) Description = ($"За результатами пошуку \"{search}\" Нічого не знайдено");
                else Description = ($"За результатами пошуку \"{search}\" знайдено");
            }

            Page();

        }

        private async Task NotesSearchInitialize()
        {
            Melody = _context.Melody.ToList();

            Music.Globals.notation = Notation.eu;

            foreach (var melody in Melody)
            {
                var wwwRootPath = Path.Combine(_environment.WebRootPath, "melodies");

                if (melody.Filepath is not null && melody.MidiMelody is null)
                {
                    var path = Path.Combine(wwwRootPath, melody.Filepath);

                    MessageL(COLORS.green, $"{melody.Title} exploring file {path}");

                    var midifile = MidiConverter.GetMidiFile(path);

                    melody.MidiMelody = await MidiConverter.GetMelodyFromMidiAsync(midifile);
                }
                else if (melody.Filepath is null)
                {
                    MessageL(COLORS.yellow, $"{melody.Title} has no file path");
                }
                else if (melody.MidiMelody is not null)
                {
                    MessageL(COLORS.yellow, $"{melody.MidiMelody} already exists");
                }

            }
        }

        
        public async Task OnPostSearchAsync()
        {
            NoteSearch = false;

            // Ініціалізую властивість midiMelody
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

            if (Melody.Count == 0) Description = ($"За результатами пошуку \"{Author}\", \"{Title}\" Нічого не знайдено");
            else Description = ($"За результатами пошуку \"{Author}\", \"{Title}\" знайдено:");

        }

        public void OnPostAsync(string key)
        {
            
                Console.WriteLine($"onPost is running {key}");
                OnPostPiano(key);
            

        }

        public IActionResult OnPostPiano(string key)
        {
            Globals.notation = Notation.eu;

            if (!string.IsNullOrEmpty(key))
            {
                Console.WriteLine($"OnPostPiano is running {key}");
                Keys += key + " ";
            }
            else
            {
                Console.WriteLine($"OnPost: empty key");                
                return Page();
            }                        
            var note = new Note(key);
            
            // відтворення ноти
            try
            {
                string mp3Path = Path.Combine(_environment.WebRootPath, "sounds", $"{key}.mp3");
                if (!System.IO.File.Exists(mp3Path))
                    GenerateMp3(note, mp3Path);
                else Console.WriteLine("using existing file");
                var relativePath = "/sounds/" + Path.GetFileName(mp3Path);
                TempData["AudioFile"] = relativePath;
                Console.WriteLine($"playing mp3 {mp3Path}");
            }
            catch (Exception ex)
            {
                Msg = ex.ToString();
            }


            return Page();
        }

        public void OnPostReset()
        {
            Console.WriteLine($"reset");
            Keys = string.Empty;
            Page();
        }


        public async Task OnPostNotesearch()
        {
            MessageL(COLORS.blue, "starting notesearch method");
            
            NoteSearch = true;

            await NotesSearchInitialize();            

            Music.Melody MelodyPattern = new();
            Globals.notation = Notation.eu;

            if (Keys is not null)
            {
                var pattern = Keys.Split(" ");
                foreach (var key in pattern)
                {
                    try
                    {
                        var note = new Note(key);
                        MelodyPattern.AddNote(note);
                    }
                    catch
                    {
                        ErrorMessage($"impossible to read note {key}\n");
                    }
                }

                /* ЛОГУВАННЯ */
                MessageL(COLORS.olive, "melodies to analyze:");
                foreach (var melody in Melody)
                    MessageL(COLORS.gray, melody.Title);
                MessageL(COLORS.olive, "notes in patten:");
                foreach (var note in MelodyPattern)
                    MessageL(COLORS.gray, note.Name());
                /*   */

                int[] patternShape = MelodyPattern.IntervalList.ToArray();
                var filteredMelodies = new List<(Melody melody, int length)>();

                MatchedMelodies.Clear();  // Очистимо перед новим пошуком

                foreach (var melody in Melody)
                {
                    if (melody.MidiMelody is null) continue;

                    var melodyshape = melody.MidiMelody.IntervalList.ToArray();
                    int commonLength = LongestCommonSubstring(patternShape, melodyshape);
                    if (commonLength >= minimummatch)
                    {
                        MatchedMelodies.Add((melody, commonLength));
                    }
                }

                // Сортуємо за довжиною збігу
                MatchedMelodies = MatchedMelodies.OrderByDescending(m => m.commonLength).ToList();

                Description = $"Знайдено {MatchedMelodies.Count} мелодій, в яких співпадають не менше ніж {minimummatch} нот поспіль";

                /*
                foreach (var melody in Melody)
                {
                    if (melody.MidiMelody is null) continue;

                    var melodyshape = melody.MidiMelody.IntervalList.ToArray();
                    int commonLength = LongestCommonSubstring(patternShape, melodyshape.ToArray());
                    if (commonLength > 0)
                    {
                        filteredMelodies.Add((melody, commonLength));
                    }
                }

                Melody = filteredMelodies.OrderByDescending(m => m.length).Select(m => m.melody).ToList();
                */

            }
            else { 
                
                Console.WriteLine("no pattern");             
            
            }
        }

    }
}
