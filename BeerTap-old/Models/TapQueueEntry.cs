namespace BeerTap.Models
{
    public class TapQueueEntry
    {
        public string UserId { get; set; }
        public string TapId { get; set; }
        public DateTime QueuedAt { get; set; }
    }

}
