namespace BeerTap.Models
{
    public class TapQueueEntry
    {
        public User User { get; set; }
        public Guid TapId { get; set; }
        public DateTime QueuedAt { get; set; }
    }

}
