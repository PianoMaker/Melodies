using System;
using System.Collections.Generic;
using System;
using System.IO;
using NAudio.Midi;
using NAudio.Wave;
using static Music.Engine;
using static Music.Globals;
using static Music.Messages;
using System.ComponentModel.Design;
using System.Drawing.Drawing2D;
//using Microsoft.DotNet.Scaffolding.Shared;
//using Microsoft.CodeAnalysis.Elfie.Serialization;

namespace Music
{

    public static class MidiConverter
    {



        public static Melody GetMelodyFromMidi(string file)
        {
            MidiFile midiFile = new MidiFile(file);
            return GetMelodyFromMidi(midiFile);
        }
        // трансформує міді-файл у формат мелодії
        public static Melody GetMelodyFromMidi(MidiFile midiFile)
        {
            MessageL(COLORS.olive, "GetMelodyFromMidi method");

            notation = Notation.eu;

            var ticksperquater = midiFile.DeltaTicksPerQuarterNote;

            Melody melody = new Melody();
            List<string> noteDurations = new List<string>(); // Для збереження тривалості нот


            foreach (var track in midiFile.Events)
            {
                int trackcounter = 0;
                Console.WriteLine($"track {trackcounter}, ticksperquater = {ticksperquater}");
                trackcounter++;
                long starttime = 0;

                foreach (var me in track)
                {
                    //темп
                    if (me is TempoEvent tempoEvent)

                        SetTempo(GetBpmFromTempoEvent(tempoEvent));
                    //власне ноти
                    if (me is NoteEvent ne)
                    {
                        if (IfNoteOn(ne))
                        {
                            starttime = ne.AbsoluteTime;
                            var pitch = ne.NoteNumber % 12;
                            var oct = ne.NoteNumber / 12 - 4;
                            var step = key_to_step(ne.NoteName);
                            var note = new Note(pitch, step, oct);
                            melody.AddNote(note);
                            //Console.Write($"note on {ne.NoteNumber} - ");                            
                        }
                        else if (IfNoteOff(ne))
                        {
                            try
                            {
                                var time = ne.AbsoluteTime - starttime;


                                var dur = 4 * (float)ticksperquater / time;
                                GrayMessageL($"input: [{ne.NoteNumber}]  {ticksperquater * 4} / {time} =  {dur}");
                                melody.Notes[melody.Notes.Count - 1].SetDuration((int)time, ticksperquater);
                                Console.WriteLine(melody.Notes[melody.Notes.Count - 1].Duration.RelDuration());
                            }
                            catch
                            {
                                ErrorMessage("impossible to set duration");
                            }
                        }
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
                MessageL(COLORS.olive, "GetMelodyFromMidiAsync is running");

                return GetMelodyFromMidi(midiFile);

            });
        }

        public static double GetBpmFromTempoEvent(TempoEvent tempoEvent)
        {
            double tempo = tempoEvent.Tempo;
            Console.WriteLine($"tempo from event = {tempo}");
            return tempo;
        }

        public static void SetTempo(double bpm)
        {

            playspeed = (int)Math.Round(48000 / bpm);
            Console.WriteLine($"tempo = {bpm}bpm, playspeed = {playspeed} ms / quater");

        }

        // трансформує MIDI файл у список нот у форматі герци-мілісекунди
        public static List<(double frequency, int durationMs)> GetHzMsListFromMidi(MidiFile midiFile)
        {
            List<(double frequency, int durationMs)> notes = new();
            Dictionary<int, double> activeNotes = new(); // {NoteNumber, StartTime в мс}
            MessageL(COLORS.blue, "starting Hz_Ms list");
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
                        double tempoBPM;
                        SetupTempo(ticksPerQuarterNote, out microsecondsPerQuarterNote, out ticksToMsFactor, tempoEvent, out tempoBPM);
                        LogTempo(microsecondsPerQuarterNote, ticksToMsFactor, tempoBPM);
                    }
                    if (midiEvent is NoteEvent ne)
                    {
                        //Console.WriteLine($"Analyzing event {ne.NoteNumber} {ne.CommandCode} {ne.Velocity}");

                        if (IfNoteOn(ne))
                        {

                            activeNotes[ne.NoteNumber] = starttime;

                            //Console.WriteLine($"\tNote On fact currentTime = {midiEvent.AbsoluteTime} vs expected {expectedcurrentticktime}");

                            if (midiEvent.AbsoluteTime > expectedcurrentticktime)
                            {
                                var pauseTickTime = midiEvent.AbsoluteTime - expectedcurrentticktime;
                                double pauseDurationMs = pauseTickTime * ticksToMsFactor;
                                notes.Add((0, (int)pauseDurationMs)); // Додаємо паузу
                                expectedcurrentticktime += pauseTickTime;
                            }
                        }
                        else if (IfNoteOff(ne))
                        {
                            //Console.WriteLine($"\tNote Off {ne.NoteNumber}");
                            if (activeNotes.TryGetValue(ne.NoteNumber, out double startTimeMs))
                            {
                                double durationMs = midiEvent.DeltaTime * ticksToMsFactor;
                                expectedcurrentticktime += midiEvent.DeltaTime;

                                double frequency = NoteToFrequency(ne.NoteNumber);

                                activeNotes.Remove(ne.NoteNumber);

                                notes.Add((frequency, (int)durationMs));

                            }
                        }
                    }

                }
            }


            notes.Add((0, 500));//для уникнення різкого обриву звучання в кінці додаємо тишу


            Console.WriteLine("result:");
            foreach (var note in notes)
            {
                Console.WriteLine($"{note.frequency} Hz - {note.durationMs} мс.");
            }


            return notes;
        }

        private static double NoteToFrequency(int noteNumber)
        {
            return 440.0 * Math.Pow(2, (noteNumber - 69) / 12.0); // A4 = 440 Hz
        }
        private static void LogTempo(double microsecondsPerQuarterNote, double ticksToMsFactor, double tempoBPM)
        {
            Console.WriteLine($"Tempo = {tempoBPM}");
            Console.WriteLine($"PQN = {microsecondsPerQuarterNote}");
            Console.WriteLine($"ticksToMsFactor  = {ticksToMsFactor}");
        }

        private static void SetupTempo(int ticksPerQuarterNote, out double microsecondsPerQuarterNote, out double ticksToMsFactor, TempoEvent tempoEvent, out double tempoBPM)
        {
            tempoBPM = tempoEvent.Tempo;
            microsecondsPerQuarterNote = 60000000.0 / tempoBPM;
            ticksToMsFactor = microsecondsPerQuarterNote / (ticksPerQuarterNote * 1000.0);
        }

        public static bool IfMonody(string midifilePath)
        {
            try
            {
                if (!File.Exists(midifilePath))
                {
                    ErrorMessage($"Невірна адреса файлу {midifilePath}");
                    return false;
                }


                var midiFile = new MidiFile(midifilePath);

                var ispoliphonic = MidiConverter.CheckForPolyphony(midiFile);


                if (ispoliphonic)
                {
                    MessageL(COLORS.red, "Виявлено поліфонію!");
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

        public static void ReadMidiFile(string path)
        {
            var mifidile = new MidiFile(path);
            int currentNoteNumber = 0;
            int currenttrack = 0;
            long currenttime = 0;
            Console.WriteLine("start reading file");
            GrayMessageL("eventType - note number - AbsTime - DeltaTime");

            foreach (var track in mifidile.Events)
            {

                Console.WriteLine($"track {currenttrack}");
                currenttrack++;
                foreach (var me in track)
                {

                    if (me is TempoEvent te)
                        Console.WriteLine(te.Tempo);
                    else if (me is NoteEvent note)
                    {
                        if (IfNoteOn(note))
                        {
                            // GrayMessageL($"\t\tafternote = {note.AbsoluteTime - currenttime}");
                            Console.WriteLine($"{note.NoteNumber} - {note.AbsoluteTime} - {note.DeltaTime}");
                            currentNoteNumber = note.NoteNumber;
                            currenttime = note.AbsoluteTime;
                        }
                        else if (IfNoteOff(note))
                        {
                            GrayMessageL($"{note.NoteNumber} - {note.AbsoluteTime} - {note.DeltaTime}");
                            // GrayMessageL($"\tduration = {note.AbsoluteTime - currenttime}");
                        }
                    }
                    else Message(COLORS.gray, ".");
                }
            }
        }

        private static bool IfNoteOff(NoteEvent note)
        {
            return note.CommandCode == MidiCommandCode.NoteOff || note.Velocity == 0;
        }

        private static bool IfNoteOn(NoteEvent note)
        {
            return note.CommandCode == MidiCommandCode.NoteOn && note.Velocity > 0;
        }

        public static int MultiStraightFile(string path)
        {
            int allchanges = 0;
            int attempt = 0;
            int currentchanges = 0;
            StraightMidiFile(path, ref currentchanges);
            while (currentchanges > 0)
            {
                allchanges += currentchanges;
                currentchanges = 0;
                attempt++;
                StraightMidiFile(path, ref currentchanges);
                if (attempt > 100) break;
            };
            return allchanges;
        }

        public static void StraightMidiFile(string path, string newpath)
        {
            var midiFile = new MidiFile(path);

            int ifchanged = 0;

            Console.WriteLine("Start straighting file");

            //MessageL(COLORS.gray, "eventType - note number - AbsTime - DeltaTime");

            var EventCollection = midiFile.Events;

            MidiEventCollection monoEventCollection = MonoEventCollection(ref ifchanged, midiFile, EventCollection);

            MidiEventCollection straightEventCollection = StraigtEventCollection(ref ifchanged, midiFile, monoEventCollection);

            MessageL(COLORS.olive, $"\n{ifchanged} have been apllied");

            MidiFile.Export(newpath, straightEventCollection);
        }

        public static void StraightMidiFile(string path)
        {
            var midiFile = new MidiFile(path);

            int ifchanged = 0;

            Console.WriteLine("Start straighting file");

            //GrayMessageL("eventType - note number - AbsTime - DeltaTime");

            var EventCollection = midiFile.Events;

            MidiEventCollection monoEventCollection = MonoEventCollection(ref ifchanged, midiFile, EventCollection);

            MidiEventCollection straightEventCollection = StraigtEventCollection(ref ifchanged, midiFile, monoEventCollection);

            MessageL(COLORS.olive, $"\n{ifchanged} have been apllied");

            MidiFile.Export(path, straightEventCollection);

        }
        public static string StraightMidiFile(string path, ref int ifchanged)
        {
            var midiFile = new MidiFile(path);

            Console.WriteLine("Start straighting file");

            //GrayMessageL("eventType - note number - AbsTime - DeltaTime");

            var EventCollection = midiFile.Events;

            MidiEventCollection monoEventCollection = MonoEventCollection(ref ifchanged, midiFile, EventCollection);

            MidiEventCollection straightEventCollection = StraigtEventCollection(ref ifchanged, midiFile, monoEventCollection);

            MessageL(COLORS.olive, $"\n{ifchanged} have been apllied");

            // Запис нового MIDI-файлу
            string newPath = path.Replace(".mid", "_straight.mid");
            MidiFile.Export(newPath, straightEventCollection);

            Console.WriteLine($"New MIDI file saved as {newPath}");

            return newPath;
        }

        private static MidiEventCollection MonoEventCollection(ref int ifchanged, MidiFile midiFile, MidiEventCollection eventCollection)
        {
            var monoEventCollection = new MidiEventCollection(midiFile.FileFormat, midiFile.DeltaTicksPerQuarterNote);
            long currentstarttime = 0;

            foreach (var track in eventCollection)
            {
                var newTrack = new List<MidiEvent>();
                Dictionary<int, long> activenotes = [];

                foreach (var me in track)
                {
                    if (me is TempoEvent tempo)
                    {
                        Message(COLORS.gray, $"{tempo}");
                        newTrack.Add(tempo); // Копіюємо інші події
                    }
                    else if (me is NoteEvent ne)
                    {
                        if (IfNoteOn(ne))
                        {
                            currentstarttime = ne.AbsoluteTime;

                            if (!activenotes.ContainsValue(ne.AbsoluteTime))
                            {
                                newTrack.Add(ne);
                                activenotes.Add(ne.NoteNumber, ne.AbsoluteTime);
                            }
                            else { MessageL(COLORS.darkred, $"removing {ne.NoteNumber}"); }

                        }
                        else if (IfNoteOff(ne))
                        {
                            if (activenotes.ContainsKey(ne.NoteNumber))
                                newTrack.Add(ne);
                            activenotes.Remove(ne.NoteNumber);
                        }
                    }
                    else
                    {
                        // Message(COLORS.gray, ".");
                        // newTrack.Add(me); // Копіюємо інші події
                    }

                }
                monoEventCollection.AddTrack(newTrack);
            }
            return monoEventCollection;
        }

        private static MidiEventCollection StraigtEventCollection(ref int ifchanged, MidiFile midiFile, MidiEventCollection EventCollection)
        {
            int noteToCorrect = 0;
            int previousNote = 0;
            int currentTrack = 0;
            long timeCorrection = 0;
            int currentchanges = 0;
            var newEventCollection = new MidiEventCollection(midiFile.FileFormat, midiFile.DeltaTicksPerQuarterNote);

            foreach (var track in EventCollection)
            {
                Console.WriteLine($"Track {currentTrack}");
                currentTrack++;
                bool isOpen = false;
                var newTrack = new List<MidiEvent>();

                foreach (var me in track)
                {
                    if (me is TempoEvent te)
                    {
                        Console.WriteLine(te.Tempo);
                        newTrack.Add(te); // Копіюємо подію у новий трек
                    }
                    else if (me is NoteEvent note)
                    {
                        if (IfNoteOn(note))
                        {
                            if (isOpen)
                            {
                                timeCorrection = note.AbsoluteTime;
                                noteToCorrect = previousNote;
                                MessageL(COLORS.darkred, $"got opennote {note.NoteNumber}");
                            }

                            Console.WriteLine($"{note.NoteNumber} - {note.AbsoluteTime} - {note.DeltaTime}");
                            long currentTime = note.AbsoluteTime;

                            newTrack.Add(note); // Копіюємо подію у новий трек

                            isOpen = true;
                            previousNote = note.NoteNumber;
                        }
                        else if (IfNoteOff(note))
                        {
                            if (timeCorrection > 0 && note.NoteNumber == noteToCorrect)
                            {
                                note.AbsoluteTime = timeCorrection - 1;
                                MessageL(COLORS.darkred, $"Time corrected for {noteToCorrect}");
                                timeCorrection = 0;
                                currentchanges++;
                                isOpen = false;
                            }
                            else if (note.NoteNumber == previousNote)
                                isOpen = false;

                            GrayMessageL($"{note.NoteNumber} - {note.AbsoluteTime} - {note.DeltaTime}");

                            //GrayMessageL($"\tduration = {note.AbsoluteTime - currentTime}");

                            newTrack.Add(note); // Копіюємо подію у новий трек

                        }
                    }
                    else
                    {
                        // Message(COLORS.gray, ".");
                        newTrack.Add(me); // Копіюємо інші події
                    }

                }


                newEventCollection.AddTrack(newTrack);
            }
            ifchanged += currentchanges;
            return newEventCollection;
        }



        // Пошук одночасно взятих нот 
        public static bool CheckForPolyphony(MidiFile midiFile)
        {
            foreach (var track in midiFile.Events)
            {
                GrayMessageL("explore track");
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


        private static void Initialize(out int channel, out MidiEventCollection events)
        {
            long absoluteTime = 0;
            channel = 1;
            int beatsPerMinute = 120;
            int patchNumber = 0;
            events = new MidiEventCollection(Globals.MidiFileType, Globals.PPQN);
            events.AddEvent(new TextEvent("C# generated stream", MetaEventType.TextEvent, absoluteTime), Globals.TrackNumber);
            ++absoluteTime;
            events.AddEvent(new TempoEvent(CalculateMicrosecondsPerQuaterNote(beatsPerMinute), absoluteTime), Globals.TrackNumber);
            events.AddEvent(new PatchChangeEvent(0, Globals.ChannelNumber, patchNumber), Globals.TrackNumber);
        }

        private static int CalculateMicrosecondsPerQuaterNote(int bpm)
        {
            return 60 * 1000 * 1000 / bpm;
        }

        private static void MelodyToTrack(Melody melody, int channel, MidiEventCollection events)
        {
            // Записуємо NoteOn події
            int noteOnTime = 0;
            foreach (var note in melody)
            {
                var noteOnEvent = new NoteOnEvent(noteOnTime, channel, note.MidiNote, 127, note.MidiDur);
                events.AddEvent(noteOnEvent, 1);
                noteOnTime += note.MidiDur;
            }

            // Записуємо NoteOff події
            int noteOffTime = 0;
            foreach (var note in melody)
            {
                noteOffTime += note.MidiDur;
                var noteOffEvent = new NoteEvent(noteOffTime, channel, MidiCommandCode.NoteOff, note.MidiNote, 0);
                events.AddEvent(noteOffEvent, 1);
            }
        }
        internal static void SaveMidi(Melody melody, string fileName = "output.mid")
        {

            int channel;
            MidiEventCollection collection;
            Initialize(out channel, out collection);
            MelodyToTrack(melody, channel, collection);
            try
            {
                collection.PrepareForExport();
                MidiFile.Export(fileName, collection);
                Console.WriteLine($"file is being saved as {Path.GetFullPath(fileName)}");
                
            }
            catch (Exception e)
            {
                Messages.ErrorMessage("Failed to save file");
                GrayMessageL(e.Message);
                
            }



        }
    }
}





