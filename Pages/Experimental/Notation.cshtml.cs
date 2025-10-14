using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Music;
using NAudio.Midi;
using System.Text.Json;
using static Music.Messages;
using MetaEvent = NAudio.Midi.MetaEvent;
using MidiEvent = NAudio.Midi.MidiEvent;
using MidiFile = NAudio.Midi.MidiFile;
using Melodies25.Data; // added for DbContext
using Melodies25.Models;

namespace Melodies25.Pages.Account
{
    public class NotationModel : PageModel
    {
        private readonly IWebHostEnvironment _env;
        private readonly Melodies25Context _context; // inject DB
        public List<string> MidiFiles { get; set; } = default!;
        public string MidiNotesJson { get; set; } = default!;

        public string CheckMessage { get; set; }

        // Map: filename.mid -> Melody.ID (if exists)
        public Dictionary<string, int> FileIds { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public NotationModel(IWebHostEnvironment env, Melodies25Context context)
        {
            _env = env;
            _context = context;
        }

        [BindProperty]
        public string SelectedMidiFile { get; set; } = default!;


        public void OnGet()
        {
            MessageL(COLORS.yellow, "Notation / OnGet");
            InitializeFiles();
        }

        private void InitializeFiles()
        {
            var midiDirectory = Path.Combine(_env.WebRootPath, "melodies");
            MidiFiles = Directory.GetFiles(midiDirectory, "*.mid")
                                 .Select(Path.GetFileName)
                                 .ToList();

            try
            {
                // Preload mapping from DB: FilePath -> ID
                if (_context != null && MidiFiles.Count > 0)
                {
                    var filesLower = MidiFiles.Select(f => f!).ToList();
                    var matched = _context.Melody
                        .Where(m => m.FilePath != null && filesLower.Contains(m.FilePath))
                        .Select(m => new { m.FilePath, m.ID })
                        .ToList();

                    FileIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    foreach (var m in matched)
                    {
                        if (!string.IsNullOrWhiteSpace(m.FilePath))
                            FileIds[m.FilePath] = m.ID;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageL(COLORS.red, $"Failed to build file->id map: {ex.Message}");
                FileIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            }
        }

        public void OnPostCheck()
        {
            MessageL(COLORS.yellow, $"Notation / OnPostCheck, {SelectedMidiFile}");
            InitializeFiles();
            try
            {
                var midiDirectory = Path.Combine(_env.WebRootPath, "melodies");
                var midiFilePath = Path.Combine(midiDirectory, SelectedMidiFile);
                var midiFile = new MidiFile(midiFilePath, false);

                for (int track = 0; track < midiFile.Tracks; track++)
                {
                    bool hasEndOfTrack = false;

                    foreach (var midiEvent in midiFile.Events[track])
                    {
                        if (midiEvent is MetaEvent metaEvent && metaEvent.MetaEventType == MetaEventType.EndTrack)
                        {
                            hasEndOfTrack = true;
                            break;
                        }
                    }

                    if (hasEndOfTrack)
                    {
                        var msg = $"Track {SelectedMidiFile}: має End of Track подію.";
                        CheckMessage = msg;
                        Console.WriteLine(msg);
                    }
                    else
                    {
                        var msg = $"Track {SelectedMidiFile}: ❗ НЕМАЄ End of Track події!";
                        CheckMessage = msg;
                        Console.WriteLine(msg);
                    }
                }
            }
            catch (Exception e)
            {
                var msg = $"помилка: {e.Message}";
                CheckMessage = msg;
                Console.WriteLine(msg);
            }
        }

        public IActionResult OnPostAddEOF()
        {
            MessageL(COLORS.yellow, $"Notation / OnPostEOF, {SelectedMidiFile}");
            InitializeFiles();

            if (string.IsNullOrEmpty(SelectedMidiFile))
            {
                ModelState.AddModelError("", "No file selected.");
                return Page();
            }

            var path = Path.Combine("wwwroot", "melodies", SelectedMidiFile);

            if (!System.IO.File.Exists(path))
            {
                ModelState.AddModelError("", "Selected file not found.");
                return Page();
            }

            try
            {
                var midiFile = new MidiFile(path, false);

                foreach (var track in midiFile.Events)
                {
                    // Створюємо новий список подій без існуючих EndTrack
                    var filteredEvents = new List<MidiEvent>();

                    foreach (var midiEvent in track)
                    {
                        if (midiEvent is MetaEvent metaEvent && metaEvent.MetaEventType == MetaEventType.EndTrack)
                            continue; // пропускаємо EndTrack
                        filteredEvents.Add(midiEvent);
                    }

                    // Додаємо новий EndTrack після останньої події
                    long lastTime = filteredEvents.Any() ? filteredEvents.Max(ev => ev.AbsoluteTime) : 0;
                    filteredEvents.Add(new MetaEvent(MetaEventType.EndTrack, 0, lastTime + 1));

                    // Очищаємо трек і додаємо нові події, відсортовані за часом
                    track.Clear();
                    foreach (var midiEvent in filteredEvents.OrderBy(ev => ev.AbsoluteTime))
                    {
                        track.Add(midiEvent);
                    }
                }

                // Перезаписати файл через MidiFile.Export
                MidiFile.Export(path, midiFile.Events);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error: {ex.Message}");
            }

            return RedirectToPage();
        }
    }
}
