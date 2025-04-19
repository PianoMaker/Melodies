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
            //string directory = Path.Combine(_environment.WebRootPath, directory);
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


        // створення mp3 файлу на основі MIDI . 
        /*
        public static void PrepareMp3(IWebHostEnvironment _environment, string midifilePath, bool ifcheck)
        {
            try
            {
                var midiPath = Path.Combine(_environment.WebRootPath, "melodies", midifilePath);
                
                string mp3Path = ConvertToMp3Path(midiPath);

                // якщо файл існує і якщо опція перевірити
                if (File.Exists(mp3Path) && ifcheck)
                {
                    MessageL(COLORS.blue, $"File {mp3Path} already exists, skip creating");
                }
                else
                {
                    // якщо не перевірити - існуючий файл перезаписується
                    var midiFile = new MidiFile(midiPath);
                    var hzmslist = GetHzMsListFromMidi(midiFile);
                    MessageL(COLORS.green, $"Starting to prepare {mp3Path}");
                    Stopwatch sw = new();
                    sw.Start();
                    GenerateMp3(hzmslist, mp3Path);
                    sw.Stop();
                    MessageL(COLORS.green, $"File {mp3Path} was generated in {sw.ElapsedMilliseconds} ms");
                }
            }
            catch (Exception ex)
            {
                //Errormsg = ex.Message;
                ErrorMessage($"Неможливо згенерувати MP3:\n {ex.Message}\n");
            }
        }
        */

        public static async Task PrepareMp3Async(IWebHostEnvironment _environment, string midifilePath, bool ifcheck)
        {
            Message(COLORS.olive, "PrepateMp3Async method: ");
            GrayMessageL(midifilePath);
            try
            {                
                //адреса мідіфайлу
                var midiPath = Path.Combine(_environment.WebRootPath, "melodies", midifilePath);
                if (!File.Exists(midiPath))
                {
                    
                    throw new Exception($"MIDI-файл відсутній");
                }

                
                //створюється назва файлу для завантаження mp3 файлу і перевірка на існування
                string mp3Path = ConvertToMp3Path(midiPath);
                if (ifcheck && File.Exists(mp3Path))
                {
                    
                    throw new Exception($"Файл вже існує");
                }
                             

                var midiFile = new MidiFile(midiPath);

                var check = 0;
                //преревірка на поліфонію
                while (CheckForPolyphony(midiFile)) 
                {

                    StraightMidiFile(midiPath);
                    check++;

                    if (check > 4)
                    { throw new Exception($"Неможливо усунути поліфонію"); }
                }
                
                // і нарешті створення!
                await PrepareMP3fromMIDIAsync(midiPath, mp3Path);

                
            }
            catch (Exception ex)
            {
                ErrorMessageL($"{ex.Message}\n");
                throw new Exception($"генерацію MP3 скасовано");
            }
            MessageL(COLORS.cyan, "PrepateMp3Async method passed");

        }

        private static async Task PrepareMP3fromMIDIAsync(string path, string mp3Path)
        {
            StraightMidiFile(path);
            StraightMidiFile(path);
            StraightMidiFile(path);
            var newFile = new MidiFile(path);
            var hzmslist = GetHzMsListFromMidi(newFile);

            MessageL(COLORS.green, $"Starting to prepare {mp3Path}");
            Stopwatch sw = new();
            sw.Start();

            await GenerateMp3Async(hzmslist, mp3Path);

            sw.Stop();
            MessageL(COLORS.green, $"File {mp3Path} was generated in {sw.ElapsedMilliseconds} ms");
        }
    }
}
