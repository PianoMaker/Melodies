using System.Text;

namespace Melodies25.Utilities
{
    public static class Translit
    {
        public static string Transliterate(string input)
        {

            Dictionary<char, string> map = new Dictionary<char, string>
        {
            {'А', "A"}, {'Б', "B"}, {'В', "V"}, {'Г', "H"}, {'Ґ', "G"}, {'Д', "D"},
            {'Е', "E"}, {'Є', "Ye"}, {'Ж', "Zh"}, {'З', "Z"}, {'И', "Y"}, {'І', "I"},
            {'Ї', "Ji"}, {'Й', "J"}, {'К', "K"}, {'Л', "L"}, {'М', "M"}, {'Н', "N"},
            {'О', "O"}, {'П', "P"}, {'Р', "R"}, {'С', "S"}, {'Т', "T"}, {'У', "U"},
            {'Ф', "F"}, {'Х', "Kh"}, {'Ц', "Ts"}, {'Ч', "Ch"}, {'Ш', "Sh"}, {'Щ', "Shch"},
            {'Ь', ""}, {'Ю', "Yu"}, {'Я', "Ya"},
            {'а', "a"}, {'б', "b"}, {'в', "v"}, {'г', "h"}, {'ґ', "g"}, {'д', "d"},
            {'е', "e"}, {'є', "ie"}, {'ж', "zh"}, {'з', "z"}, {'и', "y"}, {'і', "i"},
            {'ї', "yi"}, {'й', "j"}, {'к', "k"}, {'л', "l"}, {'м', "m"}, {'н', "n"},
            {'о', "o"}, {'п', "p"}, {'р', "r"}, {'с', "s"}, {'т', "t"}, {'у', "u"},
            {'ф', "f"}, {'х', "kh"}, {'ц', "ts"}, {'ч', "ch"}, {'ш', "sh"}, {'щ', "shch"},
            {'ь', ""}, {'ю', "yu"}, {'я', "ya"},
            {' ', "_"}, {'-', "_"}, {'.', "_"}, {',', "_"}, {'!', "_"}, {'?', "_"}
        };

            StringBuilder result = new StringBuilder();
            foreach (char c in input)
            {
                if (map.ContainsKey(c))
                    result.Append(map[c]);
                else if (char.IsLetterOrDigit(c))
                    result.Append(c);
            }

            Console.WriteLine($"{input} transliterated to {result.ToString()}");

            return result.ToString();

        }
    }
}
