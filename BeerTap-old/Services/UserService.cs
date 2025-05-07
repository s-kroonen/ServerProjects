using BeerTap.Models;

namespace BeerTap.Services
{
    public class UserService
    {
        private readonly Dictionary<string, User> _users = new();
        private readonly object _lock = new();

        public User? GetUser(string userId)
        {
            lock (_lock)
            {
                _users.TryGetValue(userId, out var user);
                return user;
            }
        }

        public void AddOrUpdateUser(User user)
        {
            lock (_lock)
            {
                _users[user.UserId] = user;
            }
        }

        public async Task UpdateUserScoreAsync(string userId, int amount)
        {
            lock (_lock)
            {
                if (_users.TryGetValue(userId, out var user))
                {
                    user.Score += amount;
                }
            }

            await Task.CompletedTask;
        }

        public async Task<bool> DeductCreditAsync(string userId)
        {
            lock (_lock)
            {
                if (_users.TryGetValue(userId, out var user) && user.Credits > 0)
                {
                    user.Credits--;
                    return true;
                }
            }

            return await Task.FromResult(false);
        }
    }
}
