using KursProject.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace KursProject.Data
{
    public class kursProjContext : DbContext
    {
        public kursProjContext(DbContextOptions<kursProjContext> options) : base(options) { }
        public DbSet<MarketingData> MarketingData { get; set; }
    }
}
