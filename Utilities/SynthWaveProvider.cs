﻿using System;
using System.Collections.Generic;
using NAudio.Wave;
using NAudio.Lame;
using System.IO;
using NAudio.Dsp;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Microsoft.DotNet.Scaffolding.Shared;

public class SynthWaveProvider : WaveProvider32
{
    private readonly List<(double Frequency, int DurationMs)> _sequence;
    private int _currentIndex = 0;
    private int _samplesRemaining;
    private double _phase;
    private double _phaseIncrement;
    private int _sampleRate;
    private int _noteIndex = 0; // Відстежуємо поточну ноту
    


    private EnvelopeGenerator _adsr;
    private float attackSeconds;
    new public WaveFormat WaveFormat { get; }
    public float AttackSeconds
    {
        get => attackSeconds;
        set
        {
            attackSeconds = value;
            _adsr.AttackRate = attackSeconds * WaveFormat.SampleRate;
        }
    }

    private float decaySeconds;
    public float DecaySeconds
    {
        get => decaySeconds;
        set
        {
            decaySeconds = value;
            _adsr.DecayRate = decaySeconds * WaveFormat.SampleRate;
        }
    }

    private float sustainLevel;
    public float SustainLevel
    {
        get => sustainLevel;
        set
        {
            sustainLevel = value;
            _adsr.SustainLevel = sustainLevel;
        }
    }

    private float releaseSeconds;
    public float ReleaseSeconds
    {
        get => releaseSeconds;

        set
        {
            releaseSeconds = value;
            _adsr.ReleaseRate = releaseSeconds * WaveFormat.SampleRate;
        }
    }

    public SynthWaveProvider(List<(double frequency, int durationMs)> sequence, int sampleRate = 44100)
        : base(sampleRate, 1) // Виклик конструктора базового класу з WaveFormat
    {
        _sequence = sequence ?? throw new ArgumentNullException(nameof(sequence)); // Перевірка на null
        _sampleRate = sampleRate;
        var channels = 1; // Mono
        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
        _adsr = new EnvelopeGenerator();

        //Defaults
        AttackSeconds = 0.01f;
        DecaySeconds = 0.4f;
        SustainLevel = 0.5f;
        ReleaseSeconds = 0.3f;

        _adsr.Gate(true);

    }


   
    public override int Read(float[] buffer, int offset, int count)
    {
        int samplesPerMs = _sampleRate / 1000;
        int index = 0;

        while (index < count && _noteIndex < _sequence.Count)
        {
            var (frequency, durationMs) = _sequence[_noteIndex];

            //паузи представлені як звуки 0 hz
            if (frequency == 0)
            {                
                if (_samplesRemaining <= 0)
                {
                    
                    _adsr.Gate(false);  // Запуск фази релізу
                    Console.WriteLine($"Release phase started for note {_noteIndex}");
                    _samplesRemaining = durationMs * samplesPerMs; // Задаємо тривалість релізу
                }
            }
            //нормальні ноти
            else
            {
                if (_samplesRemaining <= 0) // Початок нової ноти
                {
                    _phaseIncrement = 2 * Math.PI * frequency / _sampleRate;
                    _samplesRemaining = durationMs * samplesPerMs;
                    _adsr.Gate(true);  // Запуск ADSR
                    Console.WriteLine($"Starting note {_noteIndex}: {frequency} Hz, {durationMs} ms");
                }
            }

            int samplesToProcess = Math.Min(_samplesRemaining, count - index);

            //обчислення значень амплітуди для кожного семплу з урахуванням ADSR
            for (int i = 0; i < samplesToProcess; i++, index++)
            {
                float amplitude = _adsr.Process();
                buffer[offset + index] = SynthFormula(_phase) * amplitude;
                _phase += _phaseIncrement;
                if (_phase > 2 * Math.PI) _phase -= 2 * Math.PI;
            }

            _samplesRemaining -= samplesToProcess;

            if (_samplesRemaining <= 0) // Закінчили поточну ноту
            {
                _adsr.Gate(false);  // Закриття ASDR
                Console.WriteLine($"Finished note {_noteIndex}: {frequency} Hz");

                _noteIndex++; // Перехід до наступної ноти
            }
        }

        return index;
    }
    
    private float SynthFormula(double phase)
    {
        return (float)Math.Sin(_phase); // Синусоїда. 
        //Згодом слід додати інші!
    }

    // створює mp3 файл у директорії outputPath
    public static void GenerateMp3(List<(double frequency, int durationMs)> sequence, string outputPath)
    {
        int sampleRate = 44100;
        Console.WriteLine("Starting GenerateMp3 method...");

        foreach (var note in sequence)
        {
            Console.WriteLine($"Processing note: {note.frequency} Hz, {note.durationMs} ms");
        }

        //Console.WriteLine($"Trying to write to {outputPath}");

        var waveProvider = new SynthWaveProvider(sequence, sampleRate);
        //Console.WriteLine("waveProvider is ready");

        string wavPath = "output.wav";
        string mp3Path = outputPath;

        Console.WriteLine("Starting WAV file creation...");

        using (var waveStream = new WaveFileWriter(wavPath, waveProvider.WaveFormat))
        {
            byte[] buffer = new byte[Math.Min(waveProvider.WaveFormat.BlockAlign * 1024, 65536)];
            int maxBytes = sampleRate * 60 * waveProvider.WaveFormat.BlockAlign;
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

        Console.WriteLine("Starting MP3 conversion...");

        using (var reader = new WaveFileReader(wavPath))
        using (var mp3Writer = new LameMP3FileWriter(mp3Path, reader.WaveFormat, LAMEPreset.ABR_128))
        {
            reader.CopyTo(mp3Writer);
            Console.WriteLine("MP3 conversion completed.");
        }

        File.Delete(wavPath);
        Console.WriteLine("Temporary WAV file deleted.");

        Console.WriteLine($"File is ready at {outputPath}");
    }
}
/*
// Використання:
var sequence = new List<(double frequency, int durationMs)>
{
    (440, 500), // Нота A4, 500 мс
    (660, 500), // Нота E5, 500 мс
    (880, 500), // Нота A5, 500 мс
    (0, 300)    // Пауза, 300 мс
};

SynthWaveProvider.GenerateMp3(sequence, "output.mp3");
*/



/*
private void SetNextFrequency()
{
    if (_currentIndex >= _sequence.Count)
    {
        _samplesRemaining = 0;
        return;
    }

    var (freq, durationMs) = _sequence[_currentIndex];
    _phaseIncrement = 2 * Math.PI * freq / WaveFormat.SampleRate;
    _samplesRemaining = (int)((durationMs / 1000.0) * WaveFormat.SampleRate);
    _currentIndex++;
}
*/