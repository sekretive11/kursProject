using KursProject.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Reflection.Emit;

namespace KursProject.Data
{
    public class kursProjContext : DbContext
    {
        public kursProjContext(DbContextOptions<kursProjContext> options) : base(options) { }
        public DbSet<MarketingData> MarketingData { get; set; }

        /*protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MarketingData>().ToTable("marketingdata");
        }*/

    }
}
