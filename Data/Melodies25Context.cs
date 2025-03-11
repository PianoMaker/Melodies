using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using Microsoft.AspNetCore.Cors;
using Melodies25.Models;

namespace Melodies25.Data
{
    public class Melodies25Context : DbContext
    {
        public Melodies25Context (DbContextOptions<Melodies25Context> options)
            : base(options)
        {
        }

        public DbSet<Country> Country { get; set; } = default!;
        public DbSet<Author> Author { get; set; } = default!;
        public DbSet<Melody> Melody { get; set; } = default!;
    }
}
