namespace HemelBingo.Models
{
    public class BingoSession
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<User> AllowedUsers { get; set; } = new();
        public List<PulledItem> PulledItems { get; set; } = new();
    }
}
