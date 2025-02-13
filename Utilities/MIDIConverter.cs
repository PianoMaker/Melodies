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
using Microsoft.CodeAnalysis.Elfie.Serialization;

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
        // трансформує міді-файл у формат мелодії
        public static Melody GetMelodyFromMidi(MidiFile midiFile)
        {
            var ticksperquater = midiFile.DeltaTicksPerQuarterNote;


            Melody melody = new Melody();
            List<string> noteDurations = new List<string>(); // Для збереження тривалості нот


            foreach (var track in midiFile.Events)
            {
                int trackcounter = 0;

                Console.WriteLine($"track {trackcounter}");
                trackcounter++;

                foreach (var midiEvent in track)
                {
                    //темп
                    if (midiEvent is TempoEvent tempoEvent)

                        SetTempo(GetBpmFromTempoEvent(tempoEvent));
                    //власне ноти
                    if (midiEvent is NoteOnEvent noteOn)
                    {
                        var time = midiEvent.DeltaTime;
                        //Console.WriteLine("note on time = " + midiEvent.DeltaTime);

                        var pitch = noteOn.NoteNumber % 12;
                        var oct = noteOn.NoteNumber / 12 - 4;
                        var step = key_to_step(noteOn.NoteName);
                        var note = new Note(pitch, step, oct);
                        melody.AddNote(note);
                        int dur = 4 * time / ticksperquater;
                        Console.Write($"note on {noteOn.NoteNumber} - ");
                        note.SetDuration(dur);
                    }
                    else if (NoteEvent.IsNoteOff(midiEvent))
                    {
                        var time = midiEvent.DeltaTime;
                        //Console.WriteLine("note of time = " + midiEvent.DeltaTime);
                        int dur = 4 * ticksperquater / time;
                        melody.Notes[melody.Notes.Count - 1].SetDuration(dur);
                        Console.WriteLine(melody.Notes[melody.Notes.Count - 1].AbsDuration());
                    }
                }
            }

            return melody;
        }
        //те саме асинхронно
        public static async Task<Melody> GetMelodyFromMidiAsync(MidiFile midiFile)
        {
            // Використовуємо Task.Run для асинхронної обробки в окремому потоці
            return await Task.Run(() =>
            {
                var ticksperquater = midiFile.DeltaTicksPerQuarterNote;
                Melody melody = new Melody();
                Music.Globals.notation = Music.Notation.eu;
                List<string> noteDurations = new List<string>(); // Для збереження тривалості нот

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
                            Message(COLORS.gray, $"note on {noteOn.NoteNumber} - ");
                        }
                        else if (NoteEvent.IsNoteOff(midiEvent) && midiEvent.DeltaTime > 0)
                        {
                            var time = midiEvent.DeltaTime;
                            int dur = 4 * ticksperquater / time;
                            try
                            {
                                melody.Notes[melody.Notes.Count - 1].SetDuration(dur);
                            }
                            catch
                            {
                                ErrorMessage("possible duration error");
                            }
                            MessageL(COLORS.gray, $"{melody.Notes[melody.Notes.Count - 1].AbsDuration()}");
                        }
                    }
                }

                return melody;
            });
        }

        // трансформує MIDI файд у список нот у форматі герци-мілісекунди
        public static List<(double frequency, int durationMs)> GetHzMsListFromMidi(MidiFile midiFile)
        {
            List<(double frequency, int durationMs)> notes = new();
            Dictionary<int, double> activeNotes = new(); // {NoteNumber, StartTime в мс}
            MessageL(COLORS.blue, "starting hzms list");
            double starttime = 0;
            long expectedcurrentticktime = 0;
            int ticksPerQuarterNote = midiFile.DeltaTicksPerQuarterNote;
            double microsecondsPerQuarterNote = 500000; // за замовченням для 120 BPM
            double ticksToMsFactor = microsecondsPerQuarterNote / (ticksPerQuarterNote * 1000.0);

            foreach (var track in midiFile.Events)
            {
                foreach (var midiEvent in track)
                {
                    if (midiEvent is TempoEvent tempoEvent)
                    {
                        var tempoBPM = tempoEvent.Tempo;
                        Console.WriteLine($"Tempo = {tempoBPM}");
                        microsecondsPerQuarterNote = 60000000.0 / tempoBPM;
                        ticksToMsFactor = microsecondsPerQuarterNote / (ticksPerQuarterNote * 1000.0);
                        Console.WriteLine($"PQN = { microsecondsPerQuarterNote }");
                        Console.WriteLine($"ticksToMsFactor  = {ticksToMsFactor}");
                    }
                    else if (midiEvent is NoteOnEvent noteOn && noteOn.Velocity > 0)
                    {
                        activeNotes[noteOn.NoteNumber] = starttime;
                        
                       // Console.WriteLine($"fact currentTime = {midiEvent.AbsoluteTime} vs expected {expectedcurrentticktime}");

                        if (midiEvent.AbsoluteTime > expectedcurrentticktime)
                        {
                            var pauseTickTime = midiEvent.AbsoluteTime - expectedcurrentticktime;
                            double pauseDurationMs = pauseTickTime * ticksToMsFactor;
                            notes.Add((0, (int)pauseDurationMs)); // Додаємо паузу
                            expectedcurrentticktime += pauseTickTime;
                        }
                    }
                    else if (midiEvent is NoteEvent noteEvent && noteEvent.CommandCode == MidiCommandCode.NoteOff)
                    {
                        if (activeNotes.TryGetValue(noteEvent.NoteNumber, out double startTimeMs))
                        {
                            double durationMs = midiEvent.DeltaTime * ticksToMsFactor;
                            expectedcurrentticktime += midiEvent.DeltaTime;

                            double frequency = NoteToFrequency(noteEvent.NoteNumber);

                            activeNotes.Remove(noteEvent.NoteNumber);

                            notes.Add((frequency, (int)durationMs));                            
                            
                        }
                    }

                }
            }


            notes.Add((0, 500));//для уникнення різкого обриву звучання в кінці додаємо тишу
            
            /*
            Console.WriteLine("result:");
            foreach (var note in notes)
            {
                Console.WriteLine($"{note.frequency} Hz - {note.durationMs} мс.");
            }
            */

            return notes;
        }


        public static bool IfMonody(string midifilePath)
        {
            try
            {
                var midiFile = new MidiFile(midifilePath);

                var ispoliphonic = MidiConverter.CheckForPolyphony(midiFile);


                if (ispoliphonic)
                {
                    MessageL(COLORS.yellow, "Виявлено поліфонію!");
                    return false;
                }

                else
                {
                    MessageL(COLORS.blue, "Одноголосний!");
                    return true;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage($"failed to check file: {ex}");
                return false;
            }
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

                                activeNotes.Remove(noteEvent.NoteNumber);//щойно нота відзвучала вилучаємо

                                Console.WriteLine($"Нота {noteEvent.NoteNumber} ({frequency} Hz) - {duration} сек.");
                            }
                        }
                        totalTime += midiEvent.DeltaTime / (double)midiFile.DeltaTicksPerQuarterNote;//пере
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
                buffer[offset + i] = (short)(Amplitude * short.MaxValue * Math.Sin(phaseAngle));//формула
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


