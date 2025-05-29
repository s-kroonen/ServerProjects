using HemelBingo.Data;
using HemelBingo.Models;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.EntityFrameworkCore;

namespace HemelBingo.Services
{
    public class BingoSessionService
    {
        public BingoSession CurrentSesison;
        private readonly ApplicationDbContext _db;

        public readonly ProtectedSessionStorage _sessionStorage;
        private readonly ILogger<BingoSessionService> _logger;
        public BingoSessionService(ApplicationDbContext db, ProtectedSessionStorage sessionStorage, ILogger<BingoSessionService> logger)
        {
            _db = db;
            _sessionStorage = sessionStorage;
            _logger = logger;
        }
        public async Task<BingoCard?> JoinSessionAsync(int userId)
        {
            var session = await _db.Sessions
                .Include(s => s.AllowedUsers)
                .Include(s => s.PulledItems)
                .FirstOrDefaultAsync();

            if (session == null) return null;

            if (!session.AllowedUsers.Any(u => u.Id == userId))
                return null; // Not allowed

            var existingCard = await _db.Cards
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId && c.SessionId == session.Id);

            if (existingCard != null)
                return existingCard;

            var card = new BingoCard
            {
                UserId = userId,
                SessionId = session.Id,
                Items = GenerateRandomCardItems()
            };

            _db.Cards.Add(card);
            await _db.SaveChangesAsync();

            return card;
        }

        private List<BingoCardItem> GenerateRandomCardItems()
        {
            int gridSize = 5; // Example size; replace with your actual size variable
            List<BingoItem> availableItems = GetAvailableBingoItems(); // You provide this

            int totalSlots = gridSize * gridSize;
            int centerIndex = totalSlots / 2;

            var rng = new Random(); // Optionally use a seed for reproducibility

            // Fill a list with enough items by reusing if necessary
            var expandedItems = new List<BingoItem>();
            while (expandedItems.Count < totalSlots)
            {
                var shuffled = availableItems.OrderBy(x => rng.Next()).ToList();
                expandedItems.AddRange(shuffled);
            }

            // Trim to exact size
            expandedItems = expandedItems.Take(totalSlots).ToList();

            // Replace center item with a special fixed one (optional logic)
            if (gridSize % 2 == 1)
            {
                // You could define a special center item here
                var freeItem = new BingoItem { Id = -1, Name = "FREE" };
                expandedItems[centerIndex] = freeItem;
            }

            // Create BingoCardItem list
            var result = new List<BingoCardItem>();
            for (int i = 0; i < totalSlots; i++)
            {
                result.Add(new BingoCardItem
                {
                    ItemId = expandedItems[i].Id,
                    Item = expandedItems[i],
                    IsMarked = (gridSize % 2 == 1 && i == centerIndex) // auto-mark center if odd grid
                });
            }

            return result;
        }

        private List<BingoItem> GetAvailableBingoItems()
        {
            return _db.BingoItems.ToList();
        }
        public async Task<BingoCard> GetUserCardAsync(int userId, int sessionId)
        {
            // Try to fetch existing card
            var existingCard = await _db.Cards
                .Include(c => c.Items)
                .ThenInclude(ci => ci.Item)
                .FirstOrDefaultAsync(c => c.UserId == userId && c.SessionId == sessionId);

            if (existingCard != null)
                return existingCard;

            return null;
        }

    }
}
