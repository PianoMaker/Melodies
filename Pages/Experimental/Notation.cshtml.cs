using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Music;
using System.Text.Json;
using static Music.Messages;

namespace Melodies25.Pages.Account
{
    public class NotationModel : PageModel
    {
        private readonly IWebHostEnvironment _env;
        public List<string> MidiFiles { get; set; } = default!;
        public string MidiNotesJson { get; set; } = default!;

        public NotationModel(IWebHostEnvironment env)
        {
            _env = env;
        }

        [BindProperty]
        public string SelectedMidiFile { get; set; } = default!;
       

        public void OnGet()
        {
            MessageL(COLORS.yellow, "Notation / OnGet");
            var midiDirectory = Path.Combine(_env.WebRootPath, "melodies");
            MidiFiles = Directory.GetFiles(midiDirectory, "*.mid")
                                 .Select(Path.GetFileName)
                                 .ToList();
        }


        public void OnPost()
        {
            MessageL(COLORS.yellow, $"Notation / OnPost, {SelectedMidiFile}");
            var midiDirectory = Path.Combine(_env.WebRootPath, "melodies");
            MidiFiles = Directory.GetFiles(midiDirectory, "*.mid")
                     .Select(Path.GetFileName)
                     .ToList();
            
            var filePath = Path.Combine(midiDirectory, SelectedMidiFile);
            if (!System.IO.File.Exists(filePath))
            {
                ModelState.AddModelError(string.Empty, "Обраний MIDI-файл не знайдено.");
                ErrorMessageL("MIDI Файл не знайдено");
                
                return;
            }

            var midiFile = MidiFile.Read(filePath);
            var tempoMap = midiFile.GetTempoMap();
            var notes = midiFile.GetNotes();
            GrayMessageL("try to get file");
            var noteData = notes.Select(note => new
            {
                NoteNumber = note.NoteNumber,
                StartTime = note.TimeAs<MetricTimeSpan>(tempoMap),
                Duration = note.LengthAs<MetricTimeSpan>(tempoMap)
            });

            MidiNotesJson = JsonSerializer.Serialize(noteData);
        }

    }
}
