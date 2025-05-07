namespace BeerTap.Models
{
    public class User
    {
        public string UserId { get; set; } = default!;
        public string Name { get; set; } = default!;
        public int Score { get; set; }
        public int Credits { get; set; }
    }
}
