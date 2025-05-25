namespace BeerTap.Services
{
    using BeerTap.Data;
    using BeerTap.Models;
    using Microsoft.EntityFrameworkCore;

    public class TapDataService
    {
        private readonly BeerTapContext _context;

        public TapDataService(BeerTapContext context)
        {
            _context = context;
        }

        public async Task<List<Tap>> GetAllAsync() =>
            await _context.Taps.Include(t => t.TapSessions).ToListAsync();

        public async Task<Tap?> GetByIdAsync(Guid id) =>
            await _context.Taps.Include(t => t.TapSessions).FirstOrDefaultAsync(t => t.Id == id);

        public async Task AddAsync(Tap tap)
        {
            if (tap.Id == Guid.Empty)
                tap.Id = Guid.NewGuid();
            _context.Taps.Add(tap);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Tap tap)
        {
            _context.Taps.Update(tap);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var tap = await _context.Taps.FindAsync(id);
            if (tap != null)
            {
                _context.Taps.Remove(tap);
                await _context.SaveChangesAsync();
            }
        }
        public async Task<List<TapSession>> GetUserSessionsAsync(Guid userId)
        {
            return await _context.TapSessions
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.StartTime)
                .ToListAsync();
        }

        public async Task<List<TapEvent>> GetSessionEventsAsync(Guid sessionId)
        {
            return await _context.TapEvents
                .Where(e => e.SessionId == sessionId)
                .OrderBy(e => e.Timestamp)
                .ToListAsync();
        }
    }

}
