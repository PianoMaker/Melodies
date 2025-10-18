using Melodies25.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Music;
using NAudio.Midi;
using Newtonsoft.Json;
using System.Globalization;
using System.IO;
using static Music.MidiConverter;
using static Melodies25.Utilities.Algorythm;
using static Melodies25.Utilities.PrepareFiles;
using static Melodies25.Utilities.WaveConverter;
using static Music.Messages;
using Melody = Melodies25.Models.Melody;
using Melodies25.Utilities;
using System.Diagnostics;

namespace Melodies25.Pages.Melodies
{
    public class SearchModel : PageModel
    {
        private readonly Melodies25.Data.Melodies25Context _context;
        private readonly IWebHostEnvironment _environment;

        public IList<Melody> Melody { get; set; } = default!;

        // Список знайдених мелодій з довжиною збігу та позицією
        // commonLength - довжина збігу в нотах
        // position - позиція початку збігу в мелодії
        // Якщо position = -1, збіг не є підрядком (для алгоритму підпослідовності)
        // Якщо position >= 0, збіг є підрядком (для алгоритму підрядка)
        // Сортування за commonLength у спадному порядку
        // Використовується в OnPostNotesearch
        public List<(Melody melody, int commonLength, int position)> MatchedMelodies { get; set; } = new();
        // Чи відображати результати пошуку за нотами
        public bool NoteSearch { get; set; }

        [BindProperty]
        public int? TimeSignatureNumerator { get; set; }
        [BindProperty]
        public int? TimeSignatureDenominator { get; set; }
        public string Msg { get; set; }

        public string ErrorWarning { get; set; }

        public SearchModel(Melodies25.Data.Melodies25Context context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        private int minimummatch = 3;//вивести на контроллер

        [BindProperty]
        public string Note { get; set; }

        [BindProperty]
        public string Author { get; set; }

        [BindProperty]
        public string Title { get; set; }

        // Повний збіг (false) чи частковий (true)
        [BindProperty]
        public bool IfPartly { get; set; }

        [TempData]
        public string Description { get; set; }

        // Введені ноти для пошуку
        [BindProperty]
        public string Keys { get; set; }

        // Алгоритм пошуку: "Substring" (default) or "Subsequence"
        [BindProperty]
        public string SearchAlgorithm { get; set; } = "Substring";

        // Max allowed gaps for LCS (1..3)
        [BindProperty]
        public int MaxGap { get; set; } = 1;

        // Нова мелодія, створена на сторінці пошуку
        public Music.Melody NewPattern { get; set; }
        internal string TempMidiFilePath { get; set; }

        [BindProperty]
        internal string TempMp3FilePath { get; set; }

        private static readonly char[] separator = new char[] { ' ', '_' };

        public void OnGetAsync(string search, int? numerator, int? denominator)
        {

            MessageL(COLORS.yellow, "SEARCH - OnGetAsync method");

            TimeSignatureNumerator = numerator ?? 4;
            TimeSignatureDenominator = denominator ?? 4;

            if (!string.IsNullOrEmpty(search))
            {
                Console.WriteLine($"Received search: {search}");

                Melody = new List<Melody>();

                string searchQuery = search.ToLower();

                var query = _context.Melody.AsQueryable().Include(m => m.Author);


                var results = query.Where(m => m.Author != null && EF.Functions.Like(m.Author.Surname ?? "", $"%{searchQuery}%"))
                   .Concat(query.Where(m => EF.Functions.Like(m.Title ?? "", $"%{searchQuery}%")))
                   .Concat(query.Where(m => m.Author != null && EF.Functions.Like(m.Author.SurnameEn ?? "", $"%{searchQuery}%")))
                   .Concat(query.Where(m => m.Author != null && EF.Functions.Like(m.Author.NameEn ?? "", $"%{searchQuery}%")))
                   .Concat(query.Where(m => m.Author != null && EF.Functions.Like(m.Author.Name ?? "", $"%{searchQuery}%")))
                   .Distinct();

                Melody = results.ToList();


                if (Melody.Count == 0) Description = ($"За результатами пошуку \"{search}\" Нічого не знайдено");
                else Description = ($"За результатами пошуку \"{search}\" знайдено");
            }

            Page();

        }

        private async Task NotesSearchInitialize()
        {
            MessageL(COLORS.yellow, "NotesSearchInitialize Method");

            Melody = _context.Melody.Include(m => m.Author).ToList();

            Music.Globals.notation = Notation.eu;

            int numberoffileschecked = 0;
            var sw = new Stopwatch();
            sw.Start();

            var missingFiles = new List<string>();

            //Підготовка файлів
            foreach (var melody in Melody)
            {
                try
                {
                    var wwwRootPath = Path.Combine(_environment.WebRootPath, "melodies");

                    if (melody.FilePath is not null && melody.MidiMelody is null)
                    {
                        var path = Path.Combine(wwwRootPath, melody.FilePath);

                        MessageL(COLORS.green, $"{melody.Title} exploring file {path}");

                        if (!System.IO.File.Exists(path))
                        {
                            MessageL(COLORS.red, $"{path} does not exist");
                            missingFiles.Add(path);
                            // don't abort whole initialization, just skip this melody
                            continue;
                        }

                        var midifile = GetMidiFile(path);

                        melody.MidiMelody = await GetMelodyFromMidiAsync(midifile);

                        melody.MidiMelody.Enharmonize();

                        numberoffileschecked++;
                    }
                    else if (melody.FilePath is null)
                    {
                        MessageL(COLORS.yellow, $"{melody.Title} has no file path");
                    }
                    else if (melody.MidiMelody is not null)
                    {
                        MessageL(COLORS.yellow, $"{melody.MidiMelody} already exists");
                    }
                }
                catch (Exception ex)
                {
                    ErrorMessage($"Exception processing file for melody '{melody?.Title}' (FilePath='{melody?.FilePath}'): {ex.Message}");
                    Console.WriteLine($"NotesSearchInitialize: parse error for '{melody?.Title}' file='{melody?.FilePath}' => {ex}");
                    // continue with other melodies
                }
            }

            sw.Stop();
            MessageL(COLORS.standart, $"{numberoffileschecked} file analyzed, {sw.ElapsedMilliseconds} ms spent");
            if (missingFiles.Count > 0)
            {
                MessageL(COLORS.red, $"Missing {missingFiles.Count} midi files (skipped). Example: {missingFiles.First()}");
            }
            MessageL(COLORS.cyan, "NotesSearchInitialize finished");
        }


        public async Task OnPostSearchAsync()
        {
            NoteSearch = false;

            MessageL(COLORS.yellow, "SEARCH - OnPostSearchAsync method");


            // Очищення попередніх результатів
            Melody = new List<Melody>();

            IQueryable<Melody> query = _context.Melody.Include(m => m.Author);

            if (IfPartly == false)
            {
                if (!string.IsNullOrWhiteSpace(Author))
                {
                    string authorLower = Author.ToLower();
                    query = query.Where(m => m.Author.Surname.ToLower() == authorLower || m.Author.SurnameEn.ToLower() == authorLower);
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
                    query = query.Where(m => m.Author.Surname.ToLower().Contains(authorLower) || m.Author.SurnameEn.ToLower().Contains(authorLower));
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
                // Ініціалізую властивість midiMelody
                await NotesSearchInitialize();

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

            MessageL(COLORS.yellow, $"SEARCH - OnPostAsync method {key}");

            OnPostPiano(key);


        }

        public IActionResult OnPostPiano(string key)
        {
            Globals.notation = Notation.eu;

            if (!string.IsNullOrEmpty(key))
            {
                MessageL(COLORS.yellow, $"SEARCH - OnPostPiano method {key}");
                Keys += key + " ";
            }
            else
            {
                MessageL(COLORS.yellow, $"SEARCH - OnPostPiano method, no key, return");
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

        // СКИДАННЯ МЕЛОДІЇ
        public void OnPostReset()
        {
            MessageL(COLORS.yellow, $"SEARCH - OnPostReset method");
            Keys = string.Empty;
            Page();
        }

        // ПОШУК
        public async Task OnPostNotesearch()
        {
            // Diagnostics: log incoming form, bound properties and ModelState
            try
            {
                Console.WriteLine("---- OnPostNotesearch diagnostics ----");
                Console.WriteLine("Request.Method: " + Request.Method);
                Console.WriteLine("Request.Path: " + Request.Path);
                Console.WriteLine("Request.Form keys: " + string.Join(", ", Request.Form.Keys));
                Console.WriteLine($"Request.Form[TimeSignatureNumerator] = '{Request.Form["TimeSignatureNumerator"]}'");
                Console.WriteLine($"Request.Form[TimeSignatureDenominator] = '{Request.Form["TimeSignatureDenominator"]}'");
                Console.WriteLine($"Bound props before work: TimeSignatureNumerator={TimeSignatureNumerator}, TimeSignatureDenominator={TimeSignatureDenominator}");
                Console.WriteLine($"ModelState.IsValid = {ModelState.IsValid}");
                foreach (var kv in ModelState)
                {
                    var attempted = kv.Value?.AttemptedValue ?? "(null)";
                    var errors = (kv.Value?.Errors != null && kv.Value.Errors.Count > 0) ? string.Join(" | ", kv.Value.Errors.Select(e => e.ErrorMessage + (e.Exception != null ? (" (ex: " + e.Exception.Message + ")") : ""))) : "";
                    Console.WriteLine($"ModelState['{kv.Key}'] attempted='{attempted}' errors='{errors}'");
                }
                Console.WriteLine("---- end diagnostics ----");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Diagnostics failed: " + ex);
            }

            MessageL(COLORS.yellow, $"SEARCH - OnPostNotesearch method, Keys = {Keys}, TimeSin={TimeSignatureNumerator}/{TimeSignatureDenominator}");

            /*включаємо відображення за нотним пошуком*/
            NoteSearch = true;


            /*ІНІЦІАЛІЗАЦІЯ БАЗИ*/
            await NotesSearchInitialize();


            /*ІНІЦІАЛІЗАЦІЯ ВВЕДЕНОГО МАЛЮНКУ*/
            Music.Melody MelodyPattern = new();
            Globals.notation = Notation.eu;
            Globals.lng = LNG.uk;
            TempData["Keys"] = Keys;
            TempData["Numerator"] = TimeSignatureNumerator;
            TempData["Denominator"] = TimeSignatureDenominator;



            if (Keys is not null)
            {
                /* Будуємо послідовність введених нот */
                BuildPattern(MelodyPattern);
                try
                {
                    MelodyPattern.Enharmonize();
                }
                catch (Exception e)
                {
                    ErrorMessageL($"failed to enharmonize: {e}");
                }

                /* ЛОГУВАННЯ */
                //MessageL(COLORS.olive, "melodies to compare with pattern:");
                //foreach (var melody in Melody)
                //    GrayMessageL(melody.Title);
                //MessageL(COLORS.olive, "notes in patten:");
                //foreach (var note in MelodyPattern)
                //    GrayMessageL(note.Name);

                /*аудіо*/
                try
                {
                    TempMp3FilePath = PrepareTempName(_environment, ".mp3");
                    await GenerateMp3Async(MelodyPattern, TempMp3FilePath);
                    TempMp3FilePath = GetTemporaryPath(TempMp3FilePath);
                }
                catch (Exception e)
                {
                    ErrorMessageL($"impossible to create mp3: {e.Message}");
                }

                /* будуємо список виявлених збігів MathedMelodies */
                CompareMelodies(MelodyPattern);


                // Сортуємо за довжиною збігу
                MatchedMelodies = MatchedMelodies.OrderByDescending(m => m.commonLength).ToList();

                Description = $"Знайдено {MatchedMelodies.Count} мелодій, в яких співпадають не менше ніж {minimummatch} нот поспіль";

                //Передаємо список введених нот 
                ViewData["melodypattern"] = MelodyPattern.NotesList;
                ViewData["searchAlgorithm"] = SearchAlgorithm;
                GrayMessageL($"melodypattern = {MelodyPattern.NotesList}");

                //Передаємо списки нот по кожній мелодії
                CreatingPatternsView();

            }
            else
            {

                Console.WriteLine("no pattern");
                Description = "Помилка розпізнавання введеної мелодії для пошуку";
            }
            MessageL(COLORS.cyan, $"finishing SEARCH method");
        }

        private void CreatingPatternsView()
        {
            for (int i = 0; i < MatchedMelodies.Count; i++)
            {
                ViewData[$"songpattern{i}"] = MatchedMelodies[i].melody.MidiMelody?.NotesList;
                GrayMessageL($"adding song pattern {i}, {MatchedMelodies[i].melody.MidiMelody?.NotesList.Count} notes");
            }
            ViewData["matchedMelodiesCount"] = MatchedMelodies.Count;
            GrayMessageL($"patterns are ready");
        }

        private void BuildPattern(Music.Melody MelodyPattern)
        {
            MessageL(COLORS.olive, $"Building patern, keys = {Keys}");

            var pattern = Keys.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            foreach (var key in pattern)
            {
                try
                {
                    var note = new Note(key);
                    MelodyPattern.AddNote(note);
                    GrayMessage($"{note.Name} - ");
                }
                catch
                {
                    ErrorMessage($"impossible to read note {key}\n");
                }
                GrayMessageL("building is finished");
            }
        }
        // ПОРІВНЯННЯ 
        private void CompareMelodies(Music.Melody MelodyPattern)
        {
            MessageL(COLORS.olive, $"CompareMelodies method {SearchAlgorithm?.ToLowerInvariant()}");
            var sw = new Stopwatch();
            sw.Start();

            int[] patternShape = MelodyPattern.IntervalList.ToArray();
            MatchedMelodies.Clear();

            foreach (var melody in Melody)
            {
                if (melody.MidiMelody is null) continue;

                var melodyshape = melody.MidiMelody.IntervalList.ToArray();

                int length = 0;
                int position = -1;

                switch (SearchAlgorithm?.ToLowerInvariant())
                {
                    case "subsequence":
                        {
                            int clamped = Math.Clamp(MaxGap, 1, 3);
                            var (lcsLen, indices) = LongestCommonSubsequenceLimitedSkips(patternShape, melodyshape, clamped);
                            length = lcsLen;
                            position = indices.Count > 0 ? indices[0] : -1;
                            break;
                        }

                    case "substring":
                    default:
                        var (subLen, startPos) = LongestCommonSubstring(patternShape, melodyshape);
                        length = subLen;
                        position = startPos;
                        break;
                }

                if (length >= minimummatch)
                {
                    length++; // інтервалів + 1 нота
                    MatchedMelodies.Add((melody, length, position));
                }
            }
            sw.Stop();
            Console.WriteLine($"{sw.ElapsedMilliseconds} ms spent");
        }

        public IActionResult OnPostPlay(string midiPath)
        {

            MessageL(COLORS.yellow, $"SEARCH - OnPostPlay. Trying to get {midiPath}");

            try
            {
                var path = Path.Combine(_environment.WebRootPath, "melodies", midiPath);
                var midiFile = new MidiFile(path);
                var hzmslist = GetHzMsListFromMidi(midiFile);

                string mp3Path = ConvertToMp3Path(path);
                MessageL(COLORS.green, $"Starting to prepare {mp3Path}");

                GenerateMp3(hzmslist, mp3Path);
                var relativePath = "/mp3/" + Path.GetFileName(mp3Path);
                TempData["AudioFile"] = relativePath;
                MessageL(COLORS.green, relativePath);

            }
            catch (Exception ex)
            {
                ErrorWarning = ex.Message;
                ErrorMessage($"Неможливо згенерувати MP3:\n {ex.Message}\n");
            }
            return Page();
        }

        // СТВОРЕННЯ МЕЛОДІЇ ВРУЧНУ НА СТОРІНЦІ ПОШУКУ
        // зберігає у тимчасовий midi та генерує mp3 для попереднього прослуховування
        public async Task<IActionResult> OnPostMelody()
        {
            MessageL(COLORS.yellow, $"MELODIES/SEARCH - OnPostMelody method, keys = {Keys}");
            if (TempData["ErrorWarning"] is not null)
            {
                ErrorWarning = TempData["ErrorWarning"] as string ?? "";
                GrayMessageL("generating errormessage");
            }
            else GrayMessageL("errormessage is null");

            if (!string.IsNullOrWhiteSpace(Keys))
            {
                try
                {
                    Music.Melody MelodyPattern = new();
                    Globals.notation = Notation.eu;
                    Globals.lng = LNG.uk;
                    BuildPattern(MelodyPattern);
                    NewPattern = (Music.Melody)MelodyPattern.Clone();

                    TempMidiFilePath = PrepareTempName(_environment, ".mid");
                    MelodyPattern.SaveMidi(TempMidiFilePath);
                    MessageL(COLORS.green, $"temp midi saved: {TempMidiFilePath}");

                    await PrepareMp3Async(_environment, TempMidiFilePath, false);
                    TempMp3FilePath = GetTemporaryPath(ConvertToMp3Path(TempMidiFilePath));
                    TempData["HighlightPlayButton"] = true;
                    TempData["Keys"] = Keys;
                }
                catch (Exception ex)
                {
                    ErrorMessageL(ex.ToString());
                    TempData["ErrorWarning"] = "Не вдалося згенерувати файл";
                }
            }
            else
            {
                ErrorMessageL("keys are null or empty");
                TempData["ErrorWarning"] = "Жодної ноти не введено";
            }

            MessageL(COLORS.cyan, "Search.OnPostMelody finished");
            return Page();
        }


    }
}
