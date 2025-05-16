using System.Net.NetworkInformation;
using Azure.Core;
using BeerTap.Models;
using BeerTap.Repositories;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.Extensions.Logging;

namespace BeerTap.Services
{

    public class UserService
    {
        private readonly ILogger<UserService> _logger;
        private readonly UserRepository _repo;
        public readonly ProtectedSessionStorage _sessionStorage;
        private readonly TapQueueManager _tapQueueManager;

        public bool IsAuthenticated = false;
        public User? _user { get; private set; }

        public UserService(
            UserRepository repo,
            ProtectedSessionStorage sessionStorage,
            TapQueueManager tapQueueManager,
            ILogger<UserService> logger)
        {
            _repo = repo;
            _sessionStorage = sessionStorage;
            _tapQueueManager = tapQueueManager;
            _logger = logger;
        }
        public Task<int> GetScore(string userId) => _repo.GetUserScoreAsync(_user.ID);

        public Task UpdateScore(string userId, int newScore)
        {
            _user.Score = newScore;
            return _repo.UpdateUserScoreAsync(_user.ID, newScore);
        }

        public Task<int> GetAmountTapped(string userId) => _repo.GetUserAmountTappedAsync(_user.ID);

        public Task UpdateAmountTapped(string userId, float amount)
        {
            _user.AmountTapped = amount;
            return _repo.UpdateUserAmountTappedAsync(_user.ID, amount);
        }

        public async void AddAmount(string userId, float amount)
        {
            if (userId == null)
                return;
            if (amount <= 0)
                return;

            _logger.LogInformation("Adding {Amount} to user {UserId}", amount, userId);
            float oldScore = await GetAmountTapped(userId);
            float newScore = oldScore + amount;
            await UpdateAmountTapped(userId, newScore);
        }

        public async Task<bool> SignInAsync(string userId, string? pin)
        {
            _logger.LogInformation("Attempting to sign in user: {UserId}", userId);
            if (string.IsNullOrEmpty(userId))
                return false;

            var tmpUser = await _repo.GetUserAsync(userId);
            var valid = false;
            if (tmpUser != null)
            {
                _logger.LogInformation("User found in DB: {UserId}", tmpUser.UserId);
                valid = await _repo.ValidateUserAsync(userId, pin, tmpUser.PinHash);
            }

            IsAuthenticated = valid;

            if (IsAuthenticated)
            {
                _user = tmpUser;
                _logger.LogInformation("User authenticated and loaded into service: {UserId}", _user?.UserId);
            }

            if (_user != null)
            {
                await _sessionStorage.SetAsync("userId", _user.ID);
                return true;
            }

            _logger.LogWarning("Sign-in failed for user: {UserId}", userId);
            return false;
        }

        public async Task SignOut()
        {
            _logger.LogInformation("Signing out user. Current user is: {UserId}", _user?.UserId ?? "null");

            if (_user != null)
            {
                await _tapQueueManager.DequeueUserFromAllTaps(_user.UserId);
                _logger.LogInformation("User dequeued: {UserId}", _user.UserId);
            }

            _user = null;
            IsAuthenticated = false;
            await _sessionStorage.DeleteAsync("userId");

            _logger.LogInformation("Session data cleared.");
        }

        public async Task<bool> TryRestoreSessionAsync()
        {
            _logger.LogInformation("Attempting to restore session...");
            var result = await _sessionStorage.GetAsync<Guid>("userId");

            if (result.Success && result.Value != Guid.Empty)
            {
                var user = await _repo.GetUserAsync(result.Value);
                if (user != null)
                {
                    _user = user;
                    IsAuthenticated = true;
                    _logger.LogInformation("Session restored for user: {UserId}", user.UserId);
                    return true;
                }
                _logger.LogWarning("No user found for stored session ID: {UserId}", result.Value);
            }
            else
            {
                _logger.LogWarning("No valid session found.");
            }

            return false;
        }

        public async Task<bool> ValidateUser(string userId, string? pin)
        {
            var tmpUser = await _repo.GetUserAsync(userId);
            if (tmpUser != null)
            {
                _logger.LogInformation("User found in DB: {UserId}", tmpUser.UserId);
                return await _repo.ValidateUserAsync(userId, pin, tmpUser.PinHash);

            }
            return false;

        }

        public async Task<bool> UpdateUserAccount(string newUserId, string? newPin)
        {
            if (_user == null) return false;

            var updated = await _repo.UpdateUserAccountAsync(_user.ID, newUserId, newPin);
            if (updated)
            {
                _user = await _repo.GetUserAsync(_user.ID);
                return true;
            }

            return false;
        }

        public async Task<bool> SignUpAsync(string userId, string? pin)
        {
            _logger.LogInformation("Attempting to sign up user: {UserId}", userId);

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Sign-up failed: User ID was null or empty.");
                return false;
            }

            _user = await _repo.CreateUserAsync(userId, pin);

            if (_user != null)
            {
                _logger.LogInformation("User created successfully: {UserId}", _user.UserId);
                await _sessionStorage.SetAsync("userId", _user.ID);
                _logger.LogInformation("Session stored for new user: {UserId}", _user.UserId);
                return true;
            }

            _logger.LogError("Sign-up failed: Repository returned null for user creation.");
            return false;
        }

    }
}