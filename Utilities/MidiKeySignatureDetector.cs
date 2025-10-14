using System;
using System.IO;
using NAudio.Midi;
using static Music.Messages; // додано
using NuGet.Protocol.Plugins; // додано

namespace Melodies25.Utilities
{
    // Читає з MIDI подію Key Signature (FF 59 02 sf mi) і повертає "C-dur"/"a-moll" у вашому форматі
    public static class MidiKeySignatureDetector
    {
        public static string? TryDetectTonality(string midiPath)
        {
            if (string.IsNullOrWhiteSpace(midiPath) || !File.Exists(midiPath)) return null;

            // 1) Спроба через NAudio (стандартний спосіб)
            try
            {
                var mf = new MidiFile(midiPath, strictChecking: false);
                for (int track = 0; track < mf.Tracks; track++)
                {
                    foreach (var ev in mf.Events[track])
                    {
                        if (ev is KeySignatureEvent kse)
                        {
                            // kse.SharpsFlats == -7..+7; kse.MajorMinor: 0=major, 1=minor
                            var ton = MapToTonality(kse.SharpsFlats, (int)kse.MajorMinor);
                            MessageL(14, $"KSE found - {kse.SharpsFlats}:{kse.MajorMinor} = {ton}");
                            if (!string.IsNullOrWhiteSpace(ton))
                                return ton;
                        }
                    }
                }
            }
            catch
            {
                // ігноруємо, спробуємо фолбек нижче
                MessageL(14,"KeySignatureEvent via NAudio failed");
            }

            // 2) Фолбек — байтове сканування FF 59 02 sf mi
            try
            {
                var bytes = File.ReadAllBytes(midiPath);
                for (int i = 0; i < bytes.Length - 4; i++)
                {
                    // FF 59 len(=02) sf mi
                    if (bytes[i] == 0xFF && bytes[i + 1] == 0x59)
                    {
                        int lenIdx = i + 2;
                        if (lenIdx >= bytes.Length) break;

                        int len = bytes[lenIdx];
                        if (len >= 2 && lenIdx + 2 < bytes.Length)
                        {
                            sbyte sf = unchecked((sbyte)bytes[lenIdx + 1]); // -7..+7
                            byte mi = bytes[lenIdx + 2];
                            var ton = MapToTonality(sf, mi);
                            MessageL(14, $"bytes found - {sf}:{mi} = {ton}");// 0=major, 1=minor
                            return ton;
                        }
                    }
                }
            }
            catch
            {
                MessageL(14, "KeySignatureEvent via ReadAllBytes failed");
            }

            return null;
        }

  
        private static string? MapToTonality(int sf, int mi)
        {
            if (sf < -7 || sf > 7) return null;

            // sf = -7..+7 -> індекс 0..14
            int idx = sf + 7;

            // Мажор: Ces, Ges, Des, As, Es, B, F, C, G, D, A, E, H, Fis, Cis
            string[] majors = { "Ces", "Ges", "Des", "As", "Es", "B", "F", "C", "G", "D", "A", "E", "H", "Fis", "Cis" };
            // Мінор: as, es, b, f, c, g, d, a, e, h, fis, cis, gis, dis, ais
            string[] minors = { "as", "es", "b", "f", "c", "g", "d", "a", "e", "h", "fis", "cis", "gis", "dis", "ais" };

            return mi == 0 ? $"{majors[idx]}-dur" : mi == 1 ? $"{minors[idx]}-moll" : null;
        }
    }
}