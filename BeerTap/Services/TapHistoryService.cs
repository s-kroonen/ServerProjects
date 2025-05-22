using BeerTap.Data;
using BeerTap.Models;
using Microsoft.EntityFrameworkCore;

namespace BeerTap.Services
{
    public class TapHistoryService
    {
        private readonly BeerTapContext _context;

        public TapHistoryService(BeerTapContext context)
        {
            _context = context;
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
