using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Music;

namespace Melodies25.Models
{
    public class Melody
    {
        public int ID { get; set; }  // Унікальний ідентифікатор для мелодії
        public string? FilePath { get; set; }  // Назва файлу, що зберігається у wwwroot (включно з розширенням)

        public bool? IsFileEligible { get; set; } // Чи коректний файл

        public string? Tonality { get; set; } // Тональність. працювати через модель Tonality 

        
        [Required(ErrorMessage = "Назва є обов'язковою.")]
        public string Title { get; set; }  
        public int? Year { get; set; }  

        public string? Description { get; set; }  

        public int AuthorID { get; set; }  
        public Author? Author { get; set; }  // Автор пісні

       
        [Display(Name = "MIDI файл")]
        [NotMapped]
        public IFormFile? File { get; set; } 

        // власна розробка  для роботи з нотним текстом
        [NotMapped]
        public Music.Melody? MidiMelody { get; set; } 

        [NotMapped]
        public string? Mp3Filepath 
        {
            get
            {
                if (string.IsNullOrEmpty(FilePath)) return null;

                // Змінюємо розширення на .mp3
                return Path.ChangeExtension(FilePath, ".mp3");
            }
        }
    }
}
