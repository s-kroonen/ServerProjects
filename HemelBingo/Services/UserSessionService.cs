using System.Net.NetworkInformation;
using HemelBingo.Data;
using HemelBingo.Models;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace HemelBingo.Services
{
    public class UserSessionService
    {
        private readonly ApplicationDbContext _db;

        public readonly ProtectedSessionStorage _sessionStorage;
        private readonly ILogger<UserSessionService> _logger;

        public UserSessionService(ApplicationDbContext db, ProtectedSessionStorage sessionStorage, ILogger<UserSessionService> logger)
        {
            _db = db;
            _sessionStorage = sessionStorage;
            _logger = logger;
        }

        public User? CurrentUser { get; private set; }

        public bool IsLoggedIn = false;
        public string? UserName => CurrentUser?.Name;
        public string? Role => CurrentUser?.Role;

        public async Task<bool> RegisterAsync(string name, string? password)
        {
            if (await _db.Users.AnyAsync(u => u.Name == name)) return false;

            var user = new User
            {
                Name = name,
                Role = "Player",
                PasswordHash = string.IsNullOrWhiteSpace(password) ? null : BCrypt.Net.BCrypt.HashPassword(password)
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return true;
        }
        public async Task<bool> TryRestoreSessionAsync()
        {
            _logger.LogInformation("Attempting to restore session...");
            var result = await _sessionStorage.GetAsync<string>("userName");

            if (result.Success && String.IsNullOrEmpty(result.Value))
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Name == result.Value);

                if (user != null)
                {
                    CurrentUser = user;
                    IsLoggedIn = true;
                    _logger.LogInformation("Session restored for user: {name}", user.Name);
                    return true;
                }
                _logger.LogWarning("No user found for stored session ID: {name}", result.Value);
            }
            else
            {
                _logger.LogWarning("No valid session found.");
            }

            return false;
        }
        public async Task<bool> LoginAsync(string name, string? password)
        {
            //var user = await _db.Users.FirstOrDefaultAsync(u => u.Name == name);

            //if (user == null || !VerifyPin(password, user.PasswordHash)) return false;

            _logger.LogInformation("Attempting to sign in user: {name}", name);
            if (string.IsNullOrEmpty(name))
                return false;

            var tmpUser = await _db.Users.FirstOrDefaultAsync(u => u.Name == name);
            var valid = false;
            if (tmpUser != null)
            {
                _logger.LogInformation("User found in DB: {name}", tmpUser.Name);
                valid = VerifyPin(password, tmpUser.PasswordHash);
            }

            IsLoggedIn = valid;
            if (IsLoggedIn)
            {
                CurrentUser = tmpUser;
                _logger.LogInformation("User authenticated and loaded into service: {name}", CurrentUser?.Name);
            }
            if (CurrentUser != null)
            {
                await _sessionStorage.SetAsync("userName", CurrentUser.Name);
                return true;
            }
            return false;

        }

        public async void Logout()
        {

            _logger.LogInformation("Signing out user. Current user is: {UserId}", CurrentUser?.Name ?? "null");


            CurrentUser = null;
            IsLoggedIn = false;
            await _sessionStorage.DeleteAsync("userName");

            _logger.LogInformation("Session data cleared.");
        }

        private bool VerifyPin(string? pin, string? hash)
        {
            if(pin.IsNullOrEmpty() && hash.IsNullOrEmpty())
                return true;
            if (pin.IsNullOrEmpty() || hash.IsNullOrEmpty()) 
                return false;
            return BCrypt.Net.BCrypt.Verify(pin, hash);
        }
    }
}
