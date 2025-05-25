using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeerTap.Models
{
    public class TapEvent
    {
        [Key]
        public Guid Id { get; set; }
        [ForeignKey("TapSession")]
        public Guid SessionId { get; set; }
        public DateTime Timestamp { get; set; }
        public float Amount { get; set; }
        // Navigation property
        public TapSession? TapSession { get; set; }
    }
}
