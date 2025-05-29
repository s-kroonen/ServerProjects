namespace HemelBingo.Models
{
    public class BingoCard
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public BingoSession Session { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public List<BingoCardItem> Items { get; set; } = new();
    }
}
