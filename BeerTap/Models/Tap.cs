using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BeerTap.Models
{
    public class Tap
    {
        [Key]
        public Guid Id { get; set; }
        [Required] 
        public string Name { get; set; }
        [Required] 
        public string Type { get; set; }
        public string Topic { get; set; } = String.Empty;
        // Navigation property
        public List<TapSession> TapSessions { get; set; } = new();
    }
}
