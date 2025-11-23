
using Melodies25.Models;
using Melodies25.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Music;
using NAudio.Midi;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using static Music.Algorythm;
using static Melodies25.Utilities.PrepareFiles;
using static Melodies25.Utilities.WaveConverter;
using static Music.Messages;
using static Music.MidiConverter;

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
        // Додається ще список пар індексів (melodyIndex, patternIndex) для відображення підсвітки у режимі Subsequence
        // Сортування за commonLength у спадному порядку
        // Використовується в OnPostNotesearch
        public List<MatchedMelody> MatchedMelodies { get; set; } = new();
        // Чи відображати результати пошуку за нотами
        public bool NoteSearch { get; set; }

        [BindProperty]
        public int? TimeSignatureNumerator { get; set; }
        [BindProperty]
        public int? TimeSignatureDenominator { get; set; }
        public string Msg { get; set; }

        public string ErrorWarning { get; set; }

        public SearchModel(Data.Melodies25Context context, IWebHostEnvironment environment)
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
        public MusicMelody NewPattern { get; set; }

        // Тимчасові файли для створеної мелодії
        internal string TempMidiFilePath { get; set; }

        // Тимчасовий mp3 файл для створеної мелодії
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

        //--------------------------------
        // ІНІЦІАЛІЗАЦІЯ БАЗИ ДАНИХ ДЛЯ ПОШУКУ ЗА НОТАМИ
        //--------------------------------
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

        //--------------------------------
        // ПОШУК МЕЛОДІЇ ЗА МЕТАДАНИМИ
        // викликається кнопкою "Пошук" (id="metaSearchForm")
        //--------------------------------
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

        //--------------------------------
        // ДОДАВАННЯ НОТИ ЧЕРЕЗ ПІАНІНО
        // викликається кнопкою на піаніно
        // параметр key - назва ноти
        //--------------------------------
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

        //--------------------------------
        // СКИДАННЯ МЕЛОДІЇ ПОШУКУ
        // викликається кнопкою "Скинути"
        // параметр key - назва ноти
        //--------------------------------
        public void OnPostReset()
        {
            MessageL(COLORS.yellow, $"SEARCH - OnPostReset method");
            Keys = string.Empty;
            Page();
        }
        //--------------------------------
        // ПОШУК МЕЛОДІЇ ЗА НОТАМИ
        // викликається кнопкою "Пошук" (id="searchBtn")
        //--------------------------------
        public async Task OnPostNotesearch()
        {
            // Diagnostics: log incoming form, bound properties and ModelState
            DiagnosticNoteSearch();

            MessageL(COLORS.yellow, $"SEARCH - OnPostNotesearch method, Keys = {Keys}, TimeSin={TimeSignatureNumerator}/{TimeSignatureDenominator}");

            /*включаємо відображення за нотним пошуком*/
            NoteSearch = true;


            /*ІНІЦІАЛІЗАЦІЯ БАЗИ*/
            await NotesSearchInitialize();


            /*ІНІЦІАЛІЗАЦІЯ ВВЕДЕНОГО МАЛЮНКУ*/
            MusicMelody MelodyPattern = new();
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
                LogNotesearch(MelodyPattern);

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
                MatchedMelodies = MatchedMelodies.OrderByDescending(m => m.CommonLength).ToList();

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

        private void LogNotesearch(MusicMelody MelodyPattern)
        {
            MessageL(COLORS.olive, "melodies to compare with pattern:");
            foreach (var melody in Melody)
                GrayMessageL(melody.Title);
            MessageL(COLORS.olive, "notes in patten:");
            foreach (var note in MelodyPattern)
                GrayMessageL(note.Name);
        }

        private void DiagnosticNoteSearch()
        {
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
        }

        //--------------------------------
        // ВІДТВОРЕННЯ МЕЛОДІЇ
        // викликається кнопкою "Прослухати"
        //--------------------------------
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

        //--------------------------------
        // СТВОРЕННЯ МЕЛОДІЇ ВРУЧНУ НА СТОРІНЦІ ПОШУКУ
        // зберігає у тимчасовий midi та генерує mp3 для попереднього прослуховування
        // викликається кнопкою "Згенерувати мелодію"
        //--------------------------------
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
                    MusicMelody MelodyPattern = new();
                    Globals.notation = Notation.eu;
                    Globals.lng = LNG.uk;
                    BuildPattern(MelodyPattern);
                    NewPattern = (MusicMelody)MelodyPattern.Clone();

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

        //================================
        // ДОПОМІЖНІ ФУНКЦІЇ 
        //================================

        //--------------------------------
        // СТВОРЕННЯ ВІДОБРАЖЕННЯ ЗНАЙДЕНИХ ПАТЕРНІВ
        //--------------------------------
        private void CreatingPatternsView()
        {
            for (int i = 0; i < MatchedMelodies.Count; i++)
            {
                ViewData[$"songpattern{i}"] = MatchedMelodies[i].Melody.MidiMelody?.NotesList;
                // For subsequence highlighting we provide the melody indices that actually matched
                var matchedIndices = MatchedMelodies[i].Pairs?.Select(p => p.i1).ToList() ?? new List<int>();
                ViewData[$"songmatched{i}"] = matchedIndices;
                GrayMessageL($"adding song pattern {i}, {MatchedMelodies[i].Melody.MidiMelody?.NotesList.Count} notes, matched count {matchedIndices.Count}");
            }
            ViewData["matchedMelodiesCount"] = MatchedMelodies.Count;
            GrayMessageL($"patterns are ready");
        }

        //--------------------------------
        // ПОБУДОВА МЕЛОДІЇ З ВВЕДЕНИХ НОТ
        //  викликається в OnPostNotesearch
        //--------------------------------
        private void BuildPattern(MusicMelody MelodyPattern)
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
        //--------------------------------
        // ПОРІВНЯННЯ МЕЛОДІЙ
        // викликається в OnPostNotesearch
        //--------------------------------
        private void CompareMelodies(MusicMelody MelodyPattern)
        {
            MessageL(COLORS.olive, $"CompareMelodies method {SearchAlgorithm?.ToLowerInvariant()}, maxgap = {MaxGap}");
            var sw = new Stopwatch();
            sw.Start();

            int[] patternShape = MelodyPattern.IntervalList.ToArray();
            int[] patternNotes = MelodyPattern.GetPitches().ToArray();
            var algorithm = SearchAlgorithm?.ToLowerInvariant() ?? "substring";
            MatchedMelodies.Clear();

            foreach (var melody in Melody)
            {
                var title = melody.Title;
                if (melody.MidiMelody is null) continue;

                var melodyshape = melody.MidiMelody.IntervalList.ToArray(); //послідовність інтервалів
                var melodinotes = melody.MidiMelody.GetPitches().ToArray(); //послідовність нот

                int length = 0;
                int position = -1;

                // This will hold the final best pairs for the subsequence case (arr1Index, arr2Index)
                List<(int i1, int i2)> finalBestPairs = new();

                switch (algorithm)
                {
                    case "subsequence":
                        {
                            MessageL(14, $"comparing subsequence for {title}");
                            // TEMPORARY: restrict subsequence search to songs whose Title starts with Cyrillic 'Г'
                            if (string.IsNullOrEmpty(melody.Title) || !melody.Title.StartsWith("Г", StringComparison.CurrentCultureIgnoreCase))
                            {
                                MessageL(COLORS.gray, $"Skipping subsequence for '{title}' — title does not start with 'Г'");
                                // ensure no match will be registered for this melody
                                length = 0;
                                position = -1;
                                finalBestPairs = new List<(int i1, int i2)>();
                                break;
                            }

                            var (len, pos, pairs, bestShift) = MusicMelody.SimilarByIntervalsWithGap(MelodyPattern, melody.MidiMelody, MaxGap);
                            length = len;
                            position = pos;
                            finalBestPairs = pairs ?? new List<(int i1, int i2)>();
                            MessageL(COLORS.gray, $"best shift for '{title}' = {bestShift}");
                            // when adding matched melody below we will store bestShift
                            break;
                        }

                    case "substring":
                        {
                            MessageL(14, $"comparing substring for {title}");
                            var (len, pos, pairs) = MusicMelody.SimilarByIntervals(MelodyPattern, melody.MidiMelody);
                            length = len;
                            position = pos;
                            finalBestPairs = pairs ?? new List<(int i1, int i2)>();
                            break;
                        }

                    default:
                        {
                            var (len, pos, pairs) = FindLongestSubstringMatch(patternShape, melodyshape);
                            length = len;
                            position = pos;
                            finalBestPairs = pairs ?? new List<(int i1, int i2)>();
                            break;
                        }
                }

                if (length >= minimummatch)
                {
                    // If algorithm was subsequence we previously computed bestShift; recompute to capture it here
                    int bestShiftForThis = 0;
                    if (algorithm == "subsequence")
                    {
                        var (_, _, _, bestShift) = FindBestSubsequenceMatch(melodinotes, patternNotes, MaxGap);
                        bestShiftForThis = bestShift;
                    }

                    MatchedMelodies.Add(new MatchedMelody
                    {
                        Melody = melody,
                        CommonLength = length,
                        Position = position,
                        Pairs = finalBestPairs,
                        BestShift = bestShiftForThis
                    });

                    // centralized logging
                    LogFoundMatch(title, length, position, finalBestPairs);
                }
            }

            sw.Stop();
            MessageL(COLORS.yellow, $"{sw.ElapsedMilliseconds} ms spent for {algorithm} search");

            // local helper kept for minimal change - logs according to current SearchAlgorithm
            void LogFoundMatch(string title, int length, int position, List<(int i1, int i2)> pairs)
            {
                if (SearchAlgorithm?.ToLower() == "substring")
                {
                    MessageL(COLORS.cyan, $"Found match in '{title}': length={length}, position={position}");
                }
                else if (SearchAlgorithm?.ToLower() == "subsequence")
                {
                    if (pairs != null && pairs.Count > 0)
                    {
                        var sb = new System.Text.StringBuilder();
                        foreach (var p in pairs) sb.Append($"{p.i1}:{p.i2} ");
                        MessageL(COLORS.cyan, $"Found matches in '{title}': {sb.ToString().Trim()}");
                    }
                    else
                    {
                        MessageL(COLORS.cyan, $"Found matches in '{title}': (no paired indices available)");
                    }
                }
            }
        }

   
    public class MatchedMelody
    {
        public Melody Melody { get; set; } = default!;
        public int CommonLength { get; set; }
        public int Position { get; set; }
        public List<(int i1, int i2)> Pairs { get; set; } = new();
        // NEW: store optimal shift (in semitones) found for subsequence matching
        public int BestShift { get; set; } = 0;
    }
}
    }
