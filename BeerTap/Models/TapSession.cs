using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeerTap.Models
{
    public class TapSession
    {
        [Key]
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        [ForeignKey("Tap")]
        public Guid TapId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime StopTime { get; set; }
        public float TotalAmount { get; set; }
        // Navigation property
        public List<TapEvent> TapEvents { get; set; } = new();
        public Tap Tap { get; set; } = null!; // Add this line
    }
}
