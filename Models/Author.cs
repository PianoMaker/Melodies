using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Metrics;

namespace Melodies25.Models
{
    public class Author
    {
        public int ID { get; set; }  // Унікальний ідентифікатор для автора
        public string Surname { get; set; }  // Прізвище автора
        public string? Name { get; set; }  // Ім'я автора

        public string? SurnameEn { get; set; }  // Прізвище автора англ.
        public string? NameEn { get; set; }     // Ім'я автора англ.

        public Country? Country { get; set; }  // Країна автора
        public int? CountryID { get; set; }
        public int? DateOfBirth { get; set; }  // Рік народження
        public int? DateOfDeath { get; set; }  // Рік смерті 

        public string? Description { get; set; }

        // Обчислювана: якщо в імені/прізвищі є "народна пісня" або "folk song"
        [NotMapped]
        public bool IfFolkSong =>
            ContainsPhrase(Name, "народна пісня") ||
            ContainsPhrase(Surname, "народна пісня") ||
            ContainsPhrase(NameEn, "folk song") ||
            ContainsPhrase(SurnameEn, "folk song");

        [NotMapped]
        public bool IfPublicDomain
        {
            get
            {
                if (IfFolkSong) return true; // Народні пісні – сусп. надбання
                if (DateOfDeath == null) return false;
                int currentYear = DateTime.Now.Year;
                return (currentYear - DateOfDeath) > 70;
            }
        }

        [NotMapped]
        public int? MelodiesCount { get; set; }

        private static bool ContainsPhrase(string? value, string phrase) =>
            !string.IsNullOrWhiteSpace(value) &&
            value.IndexOf(phrase, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
