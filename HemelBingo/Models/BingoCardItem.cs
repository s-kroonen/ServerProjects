namespace HemelBingo.Models
{
    public class BingoCardItem
    {
        public int Id { get; set; }
        public int CardId { get; set; }
        public BingoCard Card { get; set; }

        public int ItemId { get; set; }
        public BingoItem Item { get; set; }

        public bool IsMarked { get; set; } = false;
    }
}
