using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Metrics;

namespace Melodies25.Models
{
    public class Author
    {
        public int ID { get; set; }  // Унікальний ідентифікатор для автора
        public string Surname { get; set; }  // Прізвище автора
        public string? Name { get; set; }  // Ім'я автора

        public string? SurnameEn { get; set; }  // Прізвище автора
        public string? NameEn { get; set; }  // Ім'я автора

        public Country? Country { get; set; }  // Країна автора
                
        public int? CountryID { get; set; }
        public int? DateOfBirth { get; set; }  // Рік народження
        public int? DateOfDeath { get; set; }  // Рік смерті (може бути null, якщо автор живий)

        public string? Description { get; set; }

        [NotMapped]
        public int? MelodiesCount { get; set; }
    }
}
