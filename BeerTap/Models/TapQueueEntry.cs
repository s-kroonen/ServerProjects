namespace BeerTap.Models
{
    public class TapQueueEntry
    {
        public User User { get; set; }
        public string TapId { get; set; }
        public DateTime QueuedAt { get; set; }
    }

}
