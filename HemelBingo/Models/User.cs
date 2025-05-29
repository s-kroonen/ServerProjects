namespace HemelBingo.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? PasswordHash { get; set; }
        public string Role { get; set; } // "Admin", "BingoMaster", "Player"

        public List<BingoCard> Cards { get; set; } = new();
    }

}
