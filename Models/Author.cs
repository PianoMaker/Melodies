using System.Diagnostics.Metrics;

namespace Melodies25.Models
{
    public class Author
    {
        public int ID { get; set; }  // Унікальний ідентифікатор для автора
        public string Surname { get; set; }  // Прізвище автора
        public string? Name { get; set; }  // Ім'я автора
        public Country? Country { get; set; }  // Країна автора
        
        //public string? Description { get; set; }
        public int? DateOfBirth { get; set; }  // Рік народження
        public int? DateOfDeath { get; set; }  // Рік смерті (може бути null, якщо автор живий)
    }
}
