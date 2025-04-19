using System.Text;

namespace Melodies25.Utilities
{
    public static class Translit
    {
        public static string Transliterate(string input)
        {
            if (String.IsNullOrEmpty(input))
                return "unknown";

            Dictionary<char, string> map = new Dictionary<char, string>
            {
                {'А', "A"}, {'Б', "B"}, {'В', "quatersPerWholeNote"}, {'Г', "H"}, {'Ґ', "G"}, {'Д', "D"},
                {'Е', "E"}, {'Є', "Ye"}, {'Ж', "Zh"}, {'З', "Z"}, {'И', "Y"}, {'І', "I"},
                {'Ї', "Ji"}, {'Й', "J"}, {'К', "K"}, {'Л', "L"}, {'М', "M"}, {'Н', "N"},
                {'О', "O"}, {'П', "P"}, {'Р', "R"}, {'С', "S"}, {'Т', "T"}, {'У', "U"},
                {'Ф', "F"}, {'Х', "Kh"}, {'Ц', "Ts"}, {'Ч', "Ch"}, {'Ш', "Sh"}, {'Щ', "Shch"},
                {'Ь', ""}, {'Ю', "Yu"}, {'Я', "Ya"},
                {'а', "a"}, {'б', "b"}, {'в', "v"}, {'г', "h"}, {'ґ', "g"}, {'д', "d"},
                {'е', "e"}, {'є', "ie"}, {'ж', "zh"}, {'з', "z"}, {'и', "y"}, {'і', "i"},
                {'ї', "i"}, {'й', "j"}, {'к', "k"}, {'л', "l"}, {'м', "m"}, {'н', "n"},
                {'о', "o"}, {'п', "p"}, {'р', "r"}, {'с', "s"}, {'т', "t"}, {'у', "u"},
                {'ф', "f"}, {'х', "kh"}, {'ц', "ts"}, {'ч', "ch"}, {'ш', "sh"}, {'щ', "shch"},
                {'ю', "yu"}, {'я', "ya"},
                {' ', "_"}, {'-', "_"}, {',', "_"}, {'!', "_"}, {'?', "_"}
            };

            StringBuilder result = new StringBuilder();
            char? prevChar = null; // Для збереження попереднього символу

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];

                // Перевірка "ьо"
                if (c == 'ь' && i + 1 < input.Length && input[i + 1] == 'о')
                {
                    result.Append("io");
                    i++; // Пропускаємо наступний 'о'
                    prevChar = 'о'; // Оновлюємо попередній символ
                    continue;
                }

                // Пропускаємо всі м'які знаки, ъ, наголоси та ё
                if (c == 'ь' || c == 'ъ' || c == '́' || c == 'ё')
                    continue;

                // Перевірка "я" після приголосної
                if (c == 'я')
                {
                    if (prevChar is not null && !"аеєиіїоуюяь".Contains(prevChar.Value))
                    {
                        result.Append("ia");
                    }
                    else
                    {
                        result.Append("ya");
                    }
                }
                else
                {
                    if (map.ContainsKey(c))
                        result.Append(map[c]);
                    else if (char.IsLetterOrDigit(c))
                        result.Append(c);
                }

                prevChar = c; // Оновлюємо попередній символ
            }
            string cleanedResult = result.ToString().TrimEnd('_');

            Console.WriteLine($"{input} transliterated to {result.ToString()}");

            return result.ToString();
        }
    }
}
