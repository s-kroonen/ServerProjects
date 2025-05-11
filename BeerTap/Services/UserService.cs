using BeerTap.Models;
using BeerTap.Repositories;

namespace BeerTap.Services
{
    public class UserService
    {
        private readonly UserRepository _repo;
        public User? _user { get; private set; } = new();

        public bool IsAuthenticated => !string.IsNullOrEmpty(_user.UserId);

        public UserService(UserRepository repo)
        {
            _repo = repo;
        }

        // Database methods
        public Task<User> CreateUser(string userId, string? pin)
        {
            return _repo.CreateUserAsync(userId, pin);
        }

        public Task<bool> ValidateCredentials(string userId, string? pin) => _repo.ValidateUserAsync(userId, pin);
        public Task<int> GetScore(string userId) => _repo.GetUserScoreAsync(userId);
        public Task UpdateScore(string userId, int score) => _repo.UpdateUserScoreAsync(userId, score);

        // Session methods
        public async Task<bool> SignInAsync(string userId, string? pin)
        {
            if (userId == null)
                return false;
            if (await ValidateCredentials(userId, pin))
            {
                _user.UserId = userId;
                return true;
            }

            // Try to create the user (repo handles duplicates)
            _user = await CreateUser(userId, pin);

            // Automatically sign in the user
            _user.UserId = userId;
            return true;
        }


        public void SignOut()
        {
            _user.UserId = null;
        }
    }
}
