using BeerTap.Models;
using BeerTap.Repositories;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace BeerTap.Services
{
    public class UserService
    {
        private readonly UserRepository _repo;
        private readonly ProtectedSessionStorage _sessionStorage;
        public User? _user { get; private set; }

        public bool IsAuthenticated => !string.IsNullOrEmpty(_user.UserId);

        public  UserService(UserRepository repo, ProtectedSessionStorage sessionStorage)
        {
            _repo = repo;
            _sessionStorage = sessionStorage;
        }

        // Database methods
        public Task<User> CreateUser(string userId, string? pin)
        {
            return _repo.CreateUserAsync(userId, pin);
        }

        public Task<bool> ValidateCredentials(string userId, string? pin) => _repo.ValidateUserAsync(userId, pin);
        public Task<int> GetScore(string userId) => _repo.GetUserScoreAsync(userId);
        public Task UpdateScore(string userId, int score) => _repo.UpdateUserScoreAsync(userId, score);
        public async Task<bool> SignInAsync(string userId, string? pin)
        {
            if (string.IsNullOrEmpty(userId))
                return false;

            if (await ValidateCredentials(userId, pin))
            {
                _user = await _repo.GetUserAsync(userId);
            }
            else
            {
                _user = await CreateUser(userId, pin);
            }

            if (_user != null)
            {
                await _sessionStorage.SetAsync("userId", _user.UserId);
                return true;
            }

            return false;
        }


        public async Task SignOut()
        {
            _user = null;
            await _sessionStorage.DeleteAsync("userId");
        }
        public async Task TryRestoreSessionAsync()
        {
            var result = await _sessionStorage.GetAsync<string>("userId");
            if (result.Success && !string.IsNullOrEmpty(result.Value))
            {
                var user = await _repo.GetUserAsync(result.Value);
                if (user != null)
                    _user = user;
            }
        }



    }
}