using System.ComponentModel.DataAnnotations;

namespace JobCandidates.Model
{
    public class AppUser
    {
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Range(18, 80)]
        public int Age { get; set; } = 18;

        [Required]
        [MaxLength(30)]
        public string Gender { get; set; } = "PreferNotToSay";

        [Required]
        [MaxLength(50)]
        public string Role { get; set; } = "User";

        public bool IsEmailVerified { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}