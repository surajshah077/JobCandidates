using System.ComponentModel.DataAnnotations;

namespace JobCandidates.Model
{
    public class OtpCode
    {
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string Code { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        public bool Used { get; set; } = false;
    }
}