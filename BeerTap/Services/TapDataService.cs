namespace BeerTap.Services
{
    using BeerTap.Data;
    using BeerTap.Models;
    using Microsoft.EntityFrameworkCore;

    public class TapDataService
    {
        private readonly IDbContextFactory<BeerTapContext> _contextFactory;

        public TapDataService(IDbContextFactory<BeerTapContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<Tap>> GetAllAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Taps.Include(t => t.TapSessions).ToListAsync();
        }

        public async Task<Tap?> GetByIdAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Taps.Include(t => t.TapSessions)
                                     .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task AddAsync(Tap tap)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            if (tap.Id == Guid.Empty)
                tap.Id = Guid.NewGuid();
            context.Taps.Add(tap);
            await context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Tap tap)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            context.Taps.Update(tap);
            await context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var tap = await context.Taps.FindAsync(id);
            if (tap != null)
            {
                context.Taps.Remove(tap);
                await context.SaveChangesAsync();
            }
        }

        public async Task<List<TapSession>> GetUserSessionsAsync(Guid userId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.TapSessions
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.StartTime)
                .ToListAsync();
        }

        public async Task<List<TapEvent>> GetSessionEventsAsync(Guid sessionId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.TapEvents
                .Where(e => e.SessionId == sessionId)
                .OrderBy(e => e.Timestamp)
                .ToListAsync();
        }
    }
}
