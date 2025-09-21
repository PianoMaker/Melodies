namespace Melodies25.Utilities
{
    using Melanchall.DryWetMidi.Core;
    using Melanchall.DryWetMidi.Interaction;
    using System.Text.Json;

    public class MidiNote
    {
        public int NoteNumber { get; set; }
        public long Time { get; set; }
        public long Length { get; set; }
    }

    public class MidiParser
    {
        public string ParseMidiFile(string filePath)
        {
            var midiFile = MidiFile.Read(filePath);
            var notes = midiFile.GetNotes();
            var noteList = notes.Select(note => new MidiNote
            {
                NoteNumber = note.NoteNumber,
                Time = note.Time,
                Length = note.Length
            }).ToList();

            return JsonSerializer.Serialize(noteList);
        }
    }

}
