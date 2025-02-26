﻿using Music;
using static Music.Messages;
using static Music.MidiConverter;
using static Melodies25.Utilities.WaveConverter;

using NAudio.Midi;
using System.Diagnostics;

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

        // створення mp3 файлу на основі MIDI. 
        /*
        public static void PrepareMp3(IWebHostEnvironment _environment, string midifilePath, bool ifcheck)
        {
            try
            {
                var path = Path.Combine(_environment.WebRootPath, "melodies", midifilePath);
                
                string mp3Path = ConvertToMp3Path(path);

                // якщо файл існує і якщо опція перевірити
                if (File.Exists(mp3Path) && ifcheck)
                {
                    MessageL(COLORS.blue, $"File {mp3Path} already exists, skip creating");
                }
                else
                {
                    // якщо не перевірити - існуючий файл перезаписується
                    var midiFile = new MidiFile(path);
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
            MessageL(COLORS.olive, "PrepateMp3Async method");
            try
            {
                var path = Path.Combine(_environment.WebRootPath, "melodies", midifilePath);
                if (!File.Exists(path))
                {
                    ErrorMessage($"Неможливо знайти MIDI-файл за адресою {path}");
                    return;
                }


                string mp3Path = ConvertToMp3Path(path);


                if (ifcheck && File.Exists(mp3Path))
                {

                    MessageL(COLORS.blue, $"File {mp3Path} already exists, skip creating");
                    return;
                }

                var midiFile = new MidiFile(path);
                var ifeligible = CheckForPolyphony(midiFile);

                if (!ifeligible)
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
                else
                {
                    MessageL(COLORS.red, "refused to generate mp3");
                }
            }
            catch (Exception ex)
            {
                ErrorMessage($"Неможливо згенерувати MP3:\n {ex.Message}\n");
            }
        }

    }
}
