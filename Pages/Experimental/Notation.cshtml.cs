using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Music;
using NAudio.Midi;
using System.Text.Json;
using static Music.Messages;
using MetaEvent = NAudio.Midi.MetaEvent;
using MidiFile = NAudio.Midi.MidiFile;

namespace Melodies25.Pages.Account
{
    public class NotationModel : PageModel
    {
        private readonly IWebHostEnvironment _env;
        public List<string> MidiFiles { get; set; } = default!;
        public string MidiNotesJson { get; set; } = default!;

        public string CheckMessage { get; set; }

        public NotationModel(IWebHostEnvironment env)
        {
            _env = env;
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
        }

        public void OnPost()
        {
            MessageL(COLORS.yellow, $"Notation / OnPost, {SelectedMidiFile}");
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
    }
}
