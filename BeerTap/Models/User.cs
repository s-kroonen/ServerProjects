using System.ComponentModel.DataAnnotations;
namespace BeerTap.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(3)]
        public string UserId { get; set; }
        public string? PinHash { get; set; }
        public int Score { get; set; }
    }
}