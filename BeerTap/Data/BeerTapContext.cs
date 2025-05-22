using Microsoft.EntityFrameworkCore;
using BeerTap.Models;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace BeerTap.Data
{
    public class BeerTapContext : DbContext
    {
        public BeerTapContext(DbContextOptions<BeerTapContext> options) : base(options) { }

        public DbSet<Tap> Taps { get; set; }
        public DbSet<TapSession> TapSessions { get; set; }
        public DbSet<TapEvent> TapEvents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // TapSession -> TapEvent (already there)
            modelBuilder.Entity<TapSession>()
                .HasMany(ts => ts.TapEvents)
                .WithOne(e => e.TapSession!)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Tap -> TapSession (add this)
            modelBuilder.Entity<Tap>()
                .HasMany(t => t.TapSessions)
                .WithOne(ts => ts.Tap)
                .HasForeignKey(ts => ts.TapId)
                .OnDelete(DeleteBehavior.Cascade); // optional
        }

    }
}
