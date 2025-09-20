using System;
using System.IO;

namespace Melodies25.Utilities
{
    // Читає з MIDI подію Key Signature (FF 59 02 sf mi) і повертає "C-dur"/"a-moll" у вашому форматі
    public static class MidiKeySignatureDetector
    {
        public static string? TryDetectTonality(string midiPath)
        {
            if (string.IsNullOrWhiteSpace(midiPath) || !File.Exists(midiPath)) return null;
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
                        // стандартно len == 2
                        if (len >= 2 && lenIdx + 2 < bytes.Length)
                        {
                            sbyte sf = unchecked((sbyte)bytes[lenIdx + 1]); // -7..+7
                            byte mi = bytes[lenIdx + 2];                    // 0=major, 1=minor
                            return MapToTonality(sf, mi);
                        }
                    }
                }
            }
            catch
            {
                // ігноруємо, повернемо null
            }
            return null;
        }

        // Мапінг у ваш стиль ("C-dur", "Es-dur", "a-moll", "fis-moll" тощо, з H/Fis/Cis/B тощо)
        private static string? MapToTonality(int sf, int mi)
        {
            if (sf < -7 || sf > 7) return null;

            // sf = -7..+7 -> індекс 0..14
            int idx = sf + 7;

            // Мажор: Ces, Ges, Des, As, Es, B, F, C, G, D, A, E, H, Fis, Cis
            string[] majors = { "Ces", "Ges", "Des", "As", "Es", "B", "F", "C", "G", "D", "A", "E", "H", "Fis", "Cis" };
            // Мінор: as, es, b, f, c, g, d, a, e, h, fis, cis, gis, dis, ais
            string[] minors = { "as", "es", "b", "f", "c", "g", "d", "a", "e", "h", "fis", "cis", "gis", "dis", "ais" };

            return mi == 0
                ? $"{majors[idx]}-dur"
                : mi == 1 ? $"{minors[idx]}-moll" : null;
        }
    }
}