using Music;
using static Music.Messages;
using static Music.MidiConverter;
using static Melodies25.Utilities.WaveConverter;
using NAudio.Midi;
using System.Diagnostics;
using System.IO;

namespace Melodies25.Utilities
{
    public class PrepareFiles
    {
        public static string ConvertToMp3Path(string midiPath)
        {
            string directory = Path.GetDirectoryName(midiPath)?.Replace("melodies", "mp3") ?? "";
            if (!Directory.Exists(directory) && !string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            string filenameWithoutExt = Path.GetFileNameWithoutExtension(midiPath);
            return Path.Combine(directory, filenameWithoutExt + ".mp3");
        }

        public static string GetTemporaryPath(string mp3Path)
        {
            string fileName = Path.GetFileNameWithoutExtension(mp3Path) + ".mp3";
            return "/temporary/" + fileName;
        }

        public static string PrepareTempName(IWebHostEnvironment _environment, string extension)
        {
            string filename = "userFile" + DateTime.Now.ToShortDateString() + DateTime.Now.Hour + DateTime.Now.Minute + DateTime.Now.Second;
            var tempUploads = Path.Combine(_environment.WebRootPath, "temporary");
            if (!Directory.Exists(tempUploads))
                Directory.CreateDirectory(tempUploads);
            return Path.Combine(tempUploads, filename) + extension;
        }

        // NEW: non-async wrapper if ever needed (sync generation still runs async parts via .GetAwaiter().GetResult())
        public static void PrepareMp3(IWebHostEnvironment environment, string midiFileNameOrPath, bool ifcheck)
        {
            PrepareMp3Async(environment, midiFileNameOrPath, ifcheck).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Генерує MP3 з MIDI НЕ модифікуючи оригінальний *.mid.
        /// Всі виправлення (Straight / усунення поліфонії) виконуються над копією.
        /// </summary>
        public static async Task PrepareMp3Async(IWebHostEnvironment environment, string midifileNameOrPath, bool ifcheck)
        {
            Message(COLORS.olive, "PrepareMp3Async (non destructive) start");
            try
            {
                // Визначаємо повний шлях до оригіналу
                string originalMidiPath = Path.IsPathRooted(midifileNameOrPath)
                    ? midifileNameOrPath
                    : Path.Combine(environment.WebRootPath, "melodies", midifileNameOrPath);

                if (!File.Exists(originalMidiPath))
                {
                    throw new Exception("MIDI-файл відсутній");
                }

                string mp3Path = ConvertToMp3Path(originalMidiPath);
                if (ifcheck && File.Exists(mp3Path))
                {
                    throw new Exception("Файл вже існує");
                }

                // Робоча копія у категорії temporary
                string tempDir = Path.Combine(environment.WebRootPath, "temporary");
                if (!Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                }
                string workMidiPath = Path.Combine(tempDir, "_work_" + Path.GetFileName(originalMidiPath));
                File.Copy(originalMidiPath, workMidiPath, true);

                // Усунення поліфонії на копії
                var midiFile = new MidiFile(workMidiPath);
                int safety = 0;
                while (CheckForPolyphony(midiFile))
                {
                    StraightMidiFile(workMidiPath); // модифікується ЛИШЕ копія
                    midiFile = new MidiFile(workMidiPath);
                    safety++;
                    if (safety > 4)
                        throw new Exception("Неможливо усунути поліфонію");
                }

                // Фінальна нормалізація тільки копії
                await PrepareMP3fromMIDIAsync(workMidiPath, mp3Path);

                // Видаляємо робочу копію
                if (File.Exists(workMidiPath))
                {
                    File.Delete(workMidiPath);
                    if (LoggingManager.CreateAudio)
                        MessageL(COLORS.cyan, "Temporary working copy deleted");
                }
                if (LoggingManager.CreateAudio)
                                    MessageL(COLORS.cyan, "PrepareMp3Async finished (original preserved)");
            }
            catch (Exception ex)
            {
                ErrorMessageL($"Failed to prepare mp3: {midifileNameOrPath}" + ex.Message);
                throw; // нехай викликаючий код вирішує що робити
            }
        }

        /// <summary>
        /// Внутрішній генератор MP3. ПРАЦЮЄ ТІЛЬКИ З РОБОЧОЮ КОПІЄЮ.
        /// </summary>
        private static async Task PrepareMP3fromMIDIAsync(string workingMidiPath, string mp3Path)
        {
            // Декілька "вирівнювань" застосовуються ТІЛЬКИ до копії
            StraightMidiFile(workingMidiPath);
            StraightMidiFile(workingMidiPath);
            StraightMidiFile(workingMidiPath);

            var newFile = new MidiFile(workingMidiPath);
            var hzmslist = GetHzMsListFromMidi(newFile);
            if (LoggingManager.CreateAudio)
                            MessageL(COLORS.green, $"Starting to prepare {mp3Path}");
            
            Stopwatch sw = new();
            sw.Start();

            await GenerateMp3Async(hzmslist, mp3Path);

            sw.Stop();
            if (LoggingManager.CreateAudio)
                            MessageL(COLORS.green, $"File {mp3Path} was generated in {sw.ElapsedMilliseconds} ms");
        }
    }
}
