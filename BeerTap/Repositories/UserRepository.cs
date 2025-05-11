namespace BeerTap.Repositories
{
    using Dapper;
    using Microsoft.Data.SqlClient;
    using BeerTap.Models;

    public class UserRepository
    {
        private readonly string sqlConnectionString;

        public UserRepository(string sqlConnectionString)
        {
            this.sqlConnectionString = sqlConnectionString;
        }

        public async Task<User?> CreateUserAsync(string userId, string? pin)
        {
            if (userId == null)
                return null;
            using (var sqlConnection = new SqlConnection(sqlConnectionString))
            {
                // Check if the user already exists
                var existingUser = await sqlConnection.QuerySingleOrDefaultAsync<User>(@"
                    SELECT * FROM Users 
                    WHERE UserId = @UserId",
                    new { UserId = userId });

                // If the user exists, return it
                if (existingUser != null)
                {
                    return existingUser;
                }

                // If the user does not exist, create the user
                string? pinHash = pin != null ? HashPin(pin) : null;

                var insertSql = "INSERT INTO Users (UserId, PinHash) VALUES (@UserId, @PinHash)";
                await sqlConnection.ExecuteAsync(insertSql, new { UserId = userId, PinHash = pinHash });

                // Retrieve and return the newly created user
                var newUser = await sqlConnection.QuerySingleAsync<User>(@"
                    SELECT * FROM Users 
                    WHERE UserId = @UserId",
                    new { UserId = userId });

                return newUser;
            }
        }




        public async Task<bool> ValidateUserAsync(string userId, string? pin)
        {
            using (var sqlConnection = new SqlConnection(sqlConnectionString))
            {
                var pinHash = await sqlConnection.QuerySingleOrDefaultAsync<string?>("SELECT PinHash FROM Users WHERE UserId = @UserId", new { UserId = userId });

                //if (pinHash == null && pin == null) return true;
                if (pinHash != null && pin != null) return VerifyPin(pin, pinHash);

                return false;
            }
        }

        public async Task<int> GetUserScoreAsync(string userId)
        {
            using (var sqlConnection = new SqlConnection(sqlConnectionString))
            {
                var sql = "SELECT Score FROM Users WHERE UserId = @UserId";
                return await sqlConnection.QuerySingleOrDefaultAsync<int>(sql, new { UserId = userId });
            }
        }

        public async Task UpdateUserScoreAsync(string userId, int newScore)
        {
            using (var sqlConnection = new SqlConnection(sqlConnectionString))
            {
                var sql = "UPDATE Users SET Score = @Score WHERE UserId = @UserId";
                await sqlConnection.ExecuteAsync(sql, new { UserId = userId, Score = newScore });
            }
        }

        private string HashPin(string pin)
        {
            return BCrypt.Net.BCrypt.HashPassword(pin);
        }

        private bool VerifyPin(string pin, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(pin, hash);
        }

    }

}
