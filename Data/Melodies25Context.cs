using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Melodies25.Models;

namespace Melodies25.Data
{
    public class Melodies25Context : DbContext
    {
        public Melodies25Context (DbContextOptions<Melodies25Context> options)
            : base(options)
        {
        }

        public DbSet<Melodies25.Models.Country> Country { get; set; } = default!;
        public DbSet<Melodies25.Models.Author> Author { get; set; } = default!;
        public DbSet<Melodies25.Models.Melody> Melody { get; set; } = default!;
    }
}
