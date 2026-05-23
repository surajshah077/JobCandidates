using System.ComponentModel.DataAnnotations;

namespace JobCandidates.Model
{
    public class PendingRegistration
    {
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public int Age { get; set; }

        [MaxLength(30)]
        public string Gender { get; set; } = "PreferNotToSay";

        [MaxLength(50)]
        public string Role { get; set; } = "User";

        [Required]
        public string OtpCode { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        public bool Used { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}