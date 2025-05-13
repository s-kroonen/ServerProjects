using System.ComponentModel.DataAnnotations;
namespace BeerTap.Models
{
    public class User
    {
        [Required]
        public Guid ID { get; set; }
        public required string UserId { get; set; }
        public string? PinHash { get; set; }
        public int Score { get; set; }
        public float Credits { get; set; }
        public float AmountTapped { get; set; }
    }
}