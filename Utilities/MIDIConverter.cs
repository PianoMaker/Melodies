using System;
using System.Collections.Generic;
using System;
using System.IO;
using NAudio.Midi;
using NAudio.Wave;
using static Music.Engine;
using static Music.Globals;
using static Music.Messages;
using Microsoft.DotNet.Scaffolding.Shared;

namespace Music
{

    public static class MidiConverter
    {

        public static void ReadMidi(string file)
        {
            Melody melody = GetMelodyFromMidi(file);

            // Виводимо інформацію про всі ноти в консоль
            melody.Display();
            // melody.Play();
        }

        public static Melody GetMelodyFromMidi(string file)
        {
            MidiFile midiFile = new MidiFile(file);
            return GetMelodyFromMidi(midiFile);
        }

        public static Melody GetMelodyFromMidi(MidiFile midiFile)
        {
            var ticksperquater = midiFile.DeltaTicksPerQuarterNote;


            Melody melody = new Melody();
            List<string> noteDurations = new List<string>(); // Для збереження тривалості нот
            double totalTime = 0;


            foreach (var track in midiFile.Events)
            {
                int trackcounter = 0;

                Console.WriteLine($"track {trackcounter}");
                trackcounter++;

                foreach (var midiEvent in track)
                {
                    if (midiEvent is TempoEvent tempoEvent)

                        SetTempo(GetBpmFromTempoEvent(tempoEvent));

                    if (midiEvent is NoteOnEvent noteOn)
                    {
                        // Записуємо стартову позицію ноти
                        var time = midiEvent.DeltaTime;
                        //Console.WriteLine("note on time = " + midiEvent.DeltaTime);

                        var pitch = noteOn.NoteNumber % 12;
                        var oct = noteOn.NoteNumber / 12 - 4;
                        var step = key_to_step(noteOn.NoteName);
                        var note = new Note(pitch, step, oct);
                        melody.AddNote(note);
                        //int dur = 4 * time / ticksperquater;
                        Console.Write($"note on {noteOn.NoteNumber} - ");
                        //note.SetDuration(dur);
                    }
                    else if (NoteEvent.IsNoteOff(midiEvent))
                    {
                        var time = midiEvent.DeltaTime;
                        int dur = 4 * ticksperquater / time;
                        melody.Notes[melody.Notes.Count - 1].SetDuration(dur);
                        Console.WriteLine(melody.Notes[melody.Notes.Count - 1].AbsDuration());
                    }
                }
            }

            return melody;
        }

        public static async Task<Melody> GetMelodyFromMidiAsync(MidiFile midiFile)
        {
            // Використовуємо Task.Run для асинхронної обробки в окремому потоці
            return await Task.Run(() =>
            {
                var ticksperquater = midiFile.DeltaTicksPerQuarterNote;
                Melody melody = new Melody();
                List<string> noteDurations = new List<string>(); // Для збереження тривалості нот
                double totalTime = 0;

                int trackcounter = 0;

                foreach (var track in midiFile.Events)
                {
                    Console.WriteLine($"track {trackcounter}");
                    trackcounter++;

                    foreach (var midiEvent in track)
                    {
                        if (midiEvent is TempoEvent tempoEvent)
                            SetTempo(GetBpmFromTempoEvent(tempoEvent));

                        if (midiEvent is NoteOnEvent noteOn)
                        {
                            // Записуємо стартову позицію ноти
                            var time = midiEvent.DeltaTime;

                            var pitch = noteOn.NoteNumber % 12;
                            var oct = noteOn.NoteNumber / 12 - 4;
                            var step = key_to_step(noteOn.NoteName);
                            var note = new Note(pitch, step, oct);
                            melody.AddNote(note);
                            Console.Write($"note on {noteOn.NoteNumber} - ");
                        }
                        else if (NoteEvent.IsNoteOff(midiEvent))
                        {
                            var time = midiEvent.DeltaTime;
                            int dur = 4 * ticksperquater / time;
                            melody.Notes[melody.Notes.Count - 1].SetDuration(dur);
                            Console.WriteLine(melody.Notes[melody.Notes.Count - 1].AbsDuration());
                        }
                    }
                }

                return melody;
            });
        }





        // Перетворення номеру ноти в ім'я
        private static string NoteToName(int noteNumber)
        {
            string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
            int octave = (noteNumber / 12) - 1; // Для визначення октави
            string noteName = noteNames[noteNumber % 12];
            return $"{noteName}{octave}";
        }

      

        public static string ConvertMidiToString(MidiFile midiFile, string outputDirectory)
        {
            string wavFile = Path.Combine(outputDirectory, "output.wav");

            using (var writer = new WaveFileWriter(wavFile, new WaveFormat(44100, 16, 2)))
            {
                var synth = new SineWaveProvider16();
                synth.SetWaveFormat(44100, 1); // 1 канал (моно)

                Dictionary<int, double> activeNotes = new Dictionary<int, double>(); // {NoteNumber, StartTime}
                double totalTime = 0;

                foreach (var track in midiFile.Events)
                {
                    foreach (var midiEvent in track)
                    {
                        if (midiEvent is NoteOnEvent noteOn && noteOn.Velocity > 0)
                        {
                            activeNotes[noteOn.NoteNumber] = totalTime;
                        }
                        else if (midiEvent is NoteEvent noteEvent && noteEvent.CommandCode == MidiCommandCode.NoteOff)
                        {
                            if (activeNotes.TryGetValue(noteEvent.NoteNumber, out double startTime))
                            {
                                double duration = totalTime - startTime; // Тривалість ноти в тіках

                                int frequency = NoteToFrequency(noteEvent.NoteNumber);
                                synth.Frequency = frequency;
                                synth.Amplitude = 0.5f;

                                int samples = (int)(duration * 44100); // Перетворення у семпли
                                var buffer = new short[samples];
                                synth.Read(buffer, 0, buffer.Length);
                                writer.WriteSamples(buffer, 0, buffer.Length);

                                activeNotes.Remove(noteEvent.NoteNumber);

                                Console.WriteLine($"Нота {noteEvent.NoteNumber} ({frequency} Hz) - {duration} сек.");
                            }
                        }
                        totalTime += midiEvent.DeltaTime / (double)midiFile.DeltaTicksPerQuarterNote;
                    }
                }
            }

            return wavFile;
        }


        public static MidiFile GetMidiFile(string path)
        {
            return new MidiFile(path);

        }

        // Пошук одночасно взятих нот 
        public static bool CheckForPolyphony(MidiFile midiFile)
        {
            foreach (var track in midiFile.Events)
            {
                MessageL(COLORS.gray, "explore track");
                var noteOnGroups = track
                    .OfType<NoteOnEvent>()
                    .GroupBy(e => e.AbsoluteTime)
                    .Where(g => g.Count() > 1); 

                if (noteOnGroups.Any()) 
                {
                    MessageL(COLORS.yellow, "Polyphony detected");
                    return true;
                }
            }

            MessageL(COLORS.blue, "No polyphony detected");
            return false;
        }



        public static bool CheckForPolyphony2(MidiFile midiFile)
        {//перевірка на наявність різних одночасних подій типу NoteOn
            var groupedEvents = midiFile.Events
                .OfType<NoteOnEvent>()
                .GroupBy(e => e.AbsoluteTime)
                .Where(g => g.Count() > 1); // Тільки ті, де більше 1 події в той самий момент

            if (groupedEvents.Any())
            {
                MessageL(COLORS.yellow, "polyphony detected");
                return true;
            }
            MessageL(COLORS.blue, "no polyphony detected");
            return false;
        }

        public static string ConvertMidiToWav(MidiFile midiFile, string outputDirectory)
        {
            string wavFile = Path.Combine(outputDirectory, "output.wav");

            using (var writer = new WaveFileWriter(wavFile, new WaveFormat(44100, 16, 2)))
            {
                var synth = new SineWaveProvider16();
                synth.SetWaveFormat(44100, 1); // 1 канал (моно)

                Dictionary<int, double> activeNotes = new Dictionary<int, double>(); // {NoteNumber, StartTime}
                double totalTime = 0;

                foreach (var track in midiFile.Events)
                {
                    foreach (var midiEvent in track)
                    {
                        if (midiEvent is NoteOnEvent noteOn && noteOn.Velocity > 0)
                        {
                            activeNotes[noteOn.NoteNumber] = totalTime;
                        }
                        else if (midiEvent is NoteEvent noteEvent && noteEvent.CommandCode == MidiCommandCode.NoteOff)
                        {
                            if (activeNotes.TryGetValue(noteEvent.NoteNumber, out double startTime))
                            {
                                double duration = totalTime - startTime; // Тривалість ноти в тіках

                                int frequency = NoteToFrequency(noteEvent.NoteNumber);
                                synth.Frequency = frequency;
                                synth.Amplitude = 0.5f;

                                int samples = (int)(duration * 44100); // Перетворення у семпли
                                var buffer = new short[samples];
                                synth.Read(buffer, 0, buffer.Length);
                                writer.WriteSamples(buffer, 0, buffer.Length);

                                activeNotes.Remove(noteEvent.NoteNumber);

                                Console.WriteLine($"Нота {noteEvent.NoteNumber} ({frequency} Hz) - {duration} сек.");
                            }
                        }
                        totalTime += midiEvent.DeltaTime / (double)midiFile.DeltaTicksPerQuarterNote;
                    }
                }
            }

            return wavFile;
        }

        private static int NoteToFrequency(int noteNumber)
        {
            return (int)(440.0 * Math.Pow(2, (noteNumber - 69) / 12.0)); // A4 = 440 Hz
        }
    }

    public class SineWaveProvider16 : WaveProvider16
    {
        private double phaseAngle;
        public float Frequency { get; set; }
        public float Amplitude { get; set; } = 0.5f;

        public override int Read(short[] buffer, int offset, int sampleCount)
        {
            double phaseIncrement = 2 * Math.PI * Frequency / WaveFormat.SampleRate;

            for (int i = 0; i < sampleCount; i++)
            {
                buffer[offset + i] = (short)(Amplitude * short.MaxValue * Math.Sin(phaseAngle));
                phaseAngle += phaseIncrement;
                if (phaseAngle > 2 * Math.PI) phaseAngle -= 2 * Math.PI;
            }

            return sampleCount;
        }
    }


}

public class SineWaveProvider16 : WaveProvider16
{
    private double phaseAngle;
    public float Frequency { get; set; }
    public float Amplitude { get; set; } = 0.5f;

    public override int Read(short[] buffer, int offset, int sampleCount)
    {
        double phaseIncrement = 2 * Math.PI * Frequency / WaveFormat.SampleRate;

        for (int i = 0; i < sampleCount; i++)
        {
            buffer[offset + i] = (short)(Amplitude * short.MaxValue * Math.Sin(phaseAngle));
            phaseAngle += phaseIncrement;
            if (phaseAngle > 2 * Math.PI) phaseAngle -= 2 * Math.PI;
        }

        return sampleCount;
    }

   






}


