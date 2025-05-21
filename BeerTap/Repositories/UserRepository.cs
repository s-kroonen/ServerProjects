namespace BeerTap.Repositories
{
    using Dapper;
    using Microsoft.Data.SqlClient;
    using BeerTap.Models;
    using Microsoft.IdentityModel.Tokens;

    public class UserRepository
    {
        private readonly string sqlConnectionString;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(string sqlConnectionString, ILogger<UserRepository> logger)
        {
            this.sqlConnectionString = sqlConnectionString;
            _logger = logger;
        }

        public async Task<User?> CreateUserAsync(string userId, string? pin)
        {

            using (var sqlConnection = new SqlConnection(sqlConnectionString))
            {
                // Check if the ID already exists
                var existingUser = await sqlConnection.QuerySingleOrDefaultAsync<User>(@"
                    SELECT * FROM Users 
                    WHERE UserId = @UserId",
                    new { UserId = userId });

                // If the ID exists, return it
                if (existingUser != null)
                {
                    return null;
                }

                var user = new User
                {
                    ID = Guid.NewGuid(),
                    UserId = userId,
                    PinHash = pin != null ? HashPin(pin) : null,
                    Score = 0,
                    Credits = 0,
                    AmountTapped = 0
                };

                var insertSql = @"INSERT INTO Users (ID, UserId, PinHash, Score, Credits, AmountTapped) 
                      VALUES (@ID, @UserId, @PinHash, @Score, @Credits, @AmountTapped)";

                //using var sqlConnection = new SqlConnection(_connectionString);
                await sqlConnection.ExecuteAsync(insertSql, user);

                return user;
                // Retrieve and return the newly created ID
                var newUser = await sqlConnection.QuerySingleAsync<User>(@"
                    SELECT * FROM Users 
                    WHERE UserId = @UserId",
                    new { UserId = userId });

                return newUser;
            }
        }
        public async Task<User?> GetUserAsync(string userId)
        {
            if (userId.IsNullOrEmpty())
                return null;
            using (var sqlConnection = new SqlConnection(sqlConnectionString))
            {
                var user = await sqlConnection.QuerySingleOrDefaultAsync<User>(@"
                    SELECT * FROM Users 
                    WHERE UserId = @UserId",
                    new { UserId = userId });
                return user;
            }
        }

        //public async Task<User?> GetUserAsync(Guid id)
        //{
        //    if (id == Guid.Empty)
        //        return null;
        //    using (var sqlConnection = new SqlConnection(sqlConnectionString))
        //    {
        //        var existingUser = await sqlConnection.QuerySingleOrDefaultAsync<User>(@"
        //            SELECT * FROM Users 
        //            WHERE ID = @ID",
        //            new { ID = id });
        //        return existingUser;
        //    }
        //}
        public async Task<User?> GetUserAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("Fetching ID with ID: {UserId}", id);

                using var sqlConnection = new SqlConnection(sqlConnectionString);

                var result = await sqlConnection.QuerySingleOrDefaultAsync(
                    "SELECT ID, UserId FROM Users WHERE ID = @ID", new { ID = id });

                var user = await sqlConnection.QuerySingleOrDefaultAsync<User>(
                    @"SELECT 
                        ID as ID,
                        UserId as UserId,
                        PinHash as PinHash,
                        Score as Score,
                        Credits as Credits,
                        AmountTapped as AmountTapped
                      FROM Users
                      WHERE ID = @ID", new { ID = id });


                if (user != null)
                {
                    _logger.LogInformation("User found: {UserId}", user.UserId);
                }
                else
                {
                    _logger.LogWarning("User not found with ID: {UserId}", id);
                }

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching ID with ID: {UserId}", id);
                throw;
            }
        }


        public async Task<bool> ValidateUserAsync(string userId, string? pin, string? pinHash)
        {
            using (var sqlConnection = new SqlConnection(sqlConnectionString))
            {
                _logger.LogInformation($"Looking validating pin for ID: {userId}");

                if (pinHash == null && String.IsNullOrEmpty(pin)) return true;
                if (pinHash != null && pin != null) return VerifyPin(pin, pinHash);

                return false;
            }
        }

        public async Task<int> GetUserScoreAsync(Guid ID)
        {
            using (var sqlConnection = new SqlConnection(sqlConnectionString))
            {
                var sql = "SELECT Score FROM Users WHERE ID = @ID";
                return await sqlConnection.QuerySingleOrDefaultAsync<int>(sql, new { ID });
            }
        }

        public async Task UpdateUserScoreAsync(Guid ID, float newScore)
        {
            using (var sqlConnection = new SqlConnection(sqlConnectionString))
            {
                _logger.LogInformation("Attempting to update ID {UserId} with score {amount}", ID, newScore);

                var sql = "UPDATE Users SET Score = @Score WHERE ID = @ID";
                await sqlConnection.ExecuteAsync(sql, new { ID, Score = newScore });
            }
        }

        public async Task<int> GetUserAmountTappedAsync(Guid ID)
        {
            using (var sqlConnection = new SqlConnection(sqlConnectionString))
            {
                var sql = "SELECT AmountTapped FROM Users WHERE ID = @ID";
                return await sqlConnection.QuerySingleOrDefaultAsync<int>(sql, new { ID });
            }
        }

        public async Task UpdateUserAmountTappedAsync(Guid ID, float newAmount)
        {
            using (var sqlConnection = new SqlConnection(sqlConnectionString))
            {
                _logger.LogInformation("Attempting to update ID {UserId} with amount {amount}", ID, newAmount);

                var sql = "UPDATE Users SET AmountTapped = @AmountTapped WHERE ID = @ID";
                await sqlConnection.ExecuteAsync(sql, new { ID, AmountTapped = newAmount });
            }
        }

        public async Task<bool> UpdateUserAccountAsync(Guid id, string newUserId, string? newPin)
        {
            using var sqlConnection = new SqlConnection(sqlConnectionString);

            try
            {
                _logger.LogInformation("Attempting to update ID {UserId}", id);

                string sql;
                object parameters;

                if (newPin == "")
                {
                    sql = @"UPDATE Users SET UserId = @UserId WHERE ID = @ID";
                    parameters = new { ID = id, UserId = newUserId };
                }
                else
                {
                    string? newHash = null;
                    if (newPin != null)
                        newHash = HashPin(newPin);
                    sql = @"UPDATE Users SET UserId = @UserId, PinHash = @PinHash WHERE ID = @ID";
                    parameters = new { ID = id, UserId = newUserId, PinHash = newHash };
                }

                int affectedRows = await sqlConnection.ExecuteAsync(sql, parameters);

                if (affectedRows > 0)
                {
                    _logger.LogInformation("User {UserId} updated successfully", id);
                    return true;
                }
                else
                {
                    _logger.LogWarning("User update affected 0 rows for {UserId}", id);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while updating ID account for {UserId}", id);
                return false;
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
