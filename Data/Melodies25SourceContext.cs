using Microsoft.EntityFrameworkCore;
using Melodies25.Models;

namespace Melodies25.Data
{
    public class Melodies25SourceContext : DbContext
    {
        public Melodies25SourceContext(DbContextOptions<Melodies25SourceContext> options) : base(options) { }

        public DbSet<Country> Country { get; set; } = default!;
        public DbSet<Author> Author { get; set; } = default!;
        public DbSet<Melody> Melody { get; set; } = default!;
    }
}
