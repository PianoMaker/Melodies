﻿using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Music;

namespace Melodies25.Models
{
    public class Melody
    {
        public int ID { get; set; }  // Унікальний ідентифікатор для мелодії
        public string? Filepath { get; set; }  // Назва файлу, що зберігається у wwwroot
        [Required(ErrorMessage = "Назва є обов'язковою.")]
        public string Title { get; set; }  // Назва пісні
        public int? Year { get; set; }  // Рік створення пісні

        public string? Description { get; set; }  // Опис (необов'язковий)

        public int AuthorID { get; set; }  // Зовнішній ключ для автора
        public Author? Author { get; set; }  // Автор пісні

       
        [Display(Name = "MIDI файл")]
        [NotMapped]
        public IFormFile? File { get; set; }

        // власна розробка  для роботи з нотним текстом
        [NotMapped]
        public Music.Melody? MidiMelody { get; set; }
    }
}
