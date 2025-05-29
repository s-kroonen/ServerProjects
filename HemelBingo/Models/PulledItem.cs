namespace HemelBingo.Models
{

    public class PulledItem
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public BingoSession Session { get; set; }

        public int BingoItemId { get; set; }
        public BingoItem Item { get; set; }
        public DateTime PulledAt { get; set; }
    }
}
