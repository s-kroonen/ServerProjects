using Microsoft.EntityFrameworkCore;
using BeerTap.Models;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace BeerTap.Data
{
    public class BeerTapContext : DbContext
    {
        public BeerTapContext(DbContextOptions<BeerTapContext> options) : base(options) { }

        public DbSet<TapSession> TapSessions { get; set; }
        public DbSet<TapEvent> TapEvents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TapSession>()
                .HasMany(ts => ts.TapEvents)
                .WithOne(e => e.TapSession!)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade); // optional: cascade delete
        }
    }
}
