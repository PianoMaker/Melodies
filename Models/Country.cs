using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Melodies25.Models
{
    [Index(nameof(NameUk), IsUnique = true)]
    public class Country
    {
        public int ID { get; set; }  // Унікальний ідентифікатор для автора
        
        [Required]
        public string NameUk { get; set; }  // Назва країни

        [Required]
        public string NameEn { get; set; }  // Назва країни

        [NotMapped]
        public int? AuthorsCount { get; set; }

        [NotMapped]
        public int? MelodiesCount { get; set; }
    }
}
