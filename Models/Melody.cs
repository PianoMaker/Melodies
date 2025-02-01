namespace Melodies25.Models
{
    public class Melody
    {
        public int ID { get; set; }  // Унікальний ідентифікатор для мелодії
        public string? File { get; set; }  // Назва файлу, що зберігається у wwwroot
        public string Title { get; set; }  // Назва пісні
        public int? Year { get; set; }  // Рік створення пісні

        public string? Description { get; set; }  // Опис (необов'язковий)

        public int AuthorID { get; set; }  // Зовнішній ключ для автора
        public Author Author { get; set; }  // Автор пісні
    }
}
