namespace HemelBingo.Data
{
    using HemelBingo.Models;
    using System.Collections.Generic;
    // ApplicationDbContext.cs
    using Microsoft.EntityFrameworkCore;

    public class ApplicationDbContext : DbContext
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<BingoItem> BingoItems => Set<BingoItem>();
        public DbSet<BingoSession> Sessions => Set<BingoSession>();
        public DbSet<BingoCard> Cards => Set<BingoCard>();
        public DbSet<BingoCardItem> CardItems => Set<BingoCardItem>();
        public DbSet<PulledItem> PulledItems => Set<PulledItem>();

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
    }

}
