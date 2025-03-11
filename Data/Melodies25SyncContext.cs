using Microsoft.EntityFrameworkCore;
using Melodies25.Models;

namespace Melodies25.Data
{
    public class Melodies25SyncContext : DbContext
    {
        public Melodies25SyncContext(DbContextOptions<Melodies25SyncContext> options) : base(options) { }

        public DbSet<Country> Country { get; set; } = default!;
        public DbSet<Author> Author { get; set; } = default!;
        public DbSet<Melody> Melody { get; set; } = default!;
    }
}
