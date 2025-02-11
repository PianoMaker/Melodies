using Microsoft.EntityFrameworkCore;

namespace Melodies25.Models
{
    [Index(nameof(Name), IsUnique = true)]
    public class Country
    {
        public int ID { get; set; }  // Унікальний ідентифікатор для автора
        public string Name { get; set; }  // Прізвище автора
    }
}
