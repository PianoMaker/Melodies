using Music;
using NAudio.Lame;
using NAudio.Wave;
using static Music.Engine;
using static Music.Messages;

namespace Melodies25.Utilities
{
    public static class WaveConverter
    {


        public static void GenerateWave(List<(double frequency, int durationMs)> sequence, string wavePath)
        {
            int sampleRate = 44100;

            Console.WriteLine("Starting GenerateWave method...");
            
            var waveProvider = new SynthWaveProvider(sequence, sampleRate);           
                        
            CreateWave(sampleRate, waveProvider, wavePath);            
        }

        public static async Task GenerateWaveAsync(List<(double frequency, int durationMs)> sequence, string wavePath)
        {
            int sampleRate = 44100;
            MessageL(COLORS.olive, "Starting GenerateWaveAsync method...");

            var waveProvider = new SynthWaveProvider(sequence, sampleRate);            
            await CreateWaveAsync(sampleRate, waveProvider, wavePath);            
        }


        // створює mp3 файл зі шляхом outputPath (шлях давати з розширенням .mp3)
        public static void GenerateMp3(List<(double frequency, int durationMs)> sequence, string mp3Path)
        {
            int sampleRate = 44100;
            Console.WriteLine("Starting GenerateMp3 method...");

            var waveProvider = new SynthWaveProvider(sequence, sampleRate);
            //Console.WriteLine("waveProvider is ready");

            string wavPath = "output.wav";

            CreateWave(sampleRate, waveProvider, wavPath);

            WaveToMp3(wavPath, mp3Path);
        }

        private static void WaveToMp3(string wavPath, string mp3Path)
        {
            Console.WriteLine("Starting MP3 conversion...");

            using (var reader = new WaveFileReader(wavPath))
            using (var mp3Writer = new LameMP3FileWriter(mp3Path, reader.WaveFormat, LAMEPreset.ABR_128))
            {
                reader.CopyTo(mp3Writer);
                Console.WriteLine("MP3 conversion completed.");
            }

            File.Delete(wavPath);
            Console.WriteLine("Temporary WAV file deleted.");

            Console.WriteLine($"File is ready at {mp3Path}");
        }

        private static void CreateWave(int sampleRate, SynthWaveProvider waveProvider, string wavPath)
        {
            Console.WriteLine("Starting WAV file creation...");

            using (var waveStream = new WaveFileWriter(wavPath, waveProvider.WaveFormat))
            {
                byte[] buffer = new byte[Math.Min(waveProvider.WaveFormat.BlockAlign * 1024, 65536)];
                int maxBytes = sampleRate * 120 * waveProvider.WaveFormat.BlockAlign; //обмеження у 2 хвилини
                int bytesRead;
                long totalBytesWritten = 0;

                while ((bytesRead = waveProvider.Read(buffer, 0, buffer.Length)) > 0)
                {
                    waveStream.Write(buffer, 0, bytesRead);
                    totalBytesWritten += bytesRead;
                    //Console.WriteLine($"Written {totalBytesWritten / 1024} Kbytes to WAV file...");

                    if (totalBytesWritten >= maxBytes)
                    {
                        Console.WriteLine("Reached maximum file size. Stopping.");
                        break;
                    }

                }
            }

            Console.WriteLine("WAV file created successfully.");
        }

        public static async Task GenerateMp3Async(List<(double frequency, int durationMs)> sequence, string mp3Path)
        {
            int sampleRate = 44100;
            MessageL(COLORS.olive, "Starting GenerateMp3Async method...");

            var waveProvider = new SynthWaveProvider(sequence, sampleRate);
            string wavPath = "output.wav";
            await CreateWaveAsync(sampleRate, waveProvider, wavPath);
            await WavToMp3Async(wavPath, mp3Path);
        }

        private static async Task WavToMp3Async(string wavPath, string mp3Path)
        {
            Console.WriteLine("Starting MP3 conversion...");

            using (var reader = new WaveFileReader(wavPath))
            using (var mp3Writer = new LameMP3FileWriter(mp3Path, reader.WaveFormat, LAMEPreset.ABR_128))
            {
                await reader.CopyToAsync(mp3Writer);
                Console.WriteLine("MP3 conversion completed.");
            }

            File.Delete(wavPath);
            Console.WriteLine("Temporary WAV file deleted.");
            Console.WriteLine($"File is ready at {mp3Path}");
        }

        private static async Task CreateWaveAsync(int sampleRate, SynthWaveProvider waveProvider, string wavPath)
        {
            Console.WriteLine("Starting WAV file creation...");

            using (var waveStream = new WaveFileWriter(wavPath, waveProvider.WaveFormat))
            {
                byte[] buffer = new byte[Math.Min(waveProvider.WaveFormat.BlockAlign * 1024, 65536)];
                int maxBytes = sampleRate * 120 * waveProvider.WaveFormat.BlockAlign; // обмеження у 2 хвилини
                int bytesRead;
                long totalBytesWritten = 0;

                while ((bytesRead = waveProvider.Read(buffer, 0, buffer.Length)) > 0)
                {
                    await waveStream.WriteAsync(buffer, 0, bytesRead);
                    totalBytesWritten += bytesRead;

                    if (totalBytesWritten >= maxBytes)
                    {
                        Console.WriteLine("Reached maximum file size. Stopping.");
                        break;
                    }
                }
            }

            Console.WriteLine($"WAV file {wavPath} created successfully.");
        }

        public static void GenerateMp3(Note note, string outputPath)
        {
            Console.WriteLine($"Preparing to play {note.GetName()}...");
            List<(double frequency, int durationMs)> sequence = new();
            sequence.Add(new(Pitch_to_hz(note.AbsPitch()), note.AbsDuration()));
            sequence.Add(new(0, 200));
            GenerateMp3(sequence, outputPath);
        }



        public static void GenerateMp3(MusicMelody melody, string outputPath)
        {

            List<(double frequency, int durationMs)> sequence = new();

            foreach (var note in melody.Notes)
            {

                sequence.Add(new(Pitch_to_hz(note.AbsPitch()), note.AbsDuration()));
            }
            sequence.Add(new(0, 200));
            GenerateMp3(sequence, outputPath);
        }

        public async static void GenerateMp3Async(Note note, string outputPath)
        {
            Console.WriteLine($"Preparing to play {note.GetName()}...");
            List<(double frequency, int durationMs)> sequence = new();
            sequence.Add(new(Pitch_to_hz(note.AbsPitch()), note.AbsDuration()));
            sequence.Add(new(0, 200));
            await GenerateMp3Async(sequence, outputPath);
        }

        public async static Task GenerateMp3Async(MusicMelody melody, string outputPath)
        {

            List<(double frequency, int durationMs)> sequence = new();

            foreach (var note in melody.Notes)
            {

                sequence.Add(new(Pitch_to_hz(note.AbsPitch()), note.AbsDuration()));
            }
            sequence.Add(new(0, 200));
            await GenerateMp3Async(sequence, outputPath);
        }
    }
}
