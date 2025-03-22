using Microsoft.EntityFrameworkCore;
using Melodies25.Models;

//target

namespace Melodies25.Data
{
    public class Melodies25TargetContext : DbContext
    {
        public Melodies25TargetContext(DbContextOptions<Melodies25TargetContext> options) : base(options) { }

        public DbSet<Country> Country { get; set; } = default!;
        public DbSet<Author> Author { get; set; } = default!;
        public DbSet<Melody> Melody { get; set; } = default!;
    }
}
