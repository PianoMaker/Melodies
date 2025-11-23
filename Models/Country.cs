using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Melodies25.Models
{
    [Index(nameof(Name), IsUnique = true)]
    public class Country
    {
        public int ID { get; set; }  // Унікальний ідентифікатор для автора
        
        [Required]
        public string Name { get; set; }  // Назва країни

        [NotMapped]
        public int? AuthorsCount { get; set; }

        [NotMapped]
        public int? MelodiesCount { get; set; }
    }
}
