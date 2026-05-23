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

        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        // Role examples: "Admin", "Recruiter", "User"
        [Required]
        [MaxLength(50)]
        public string Role { get; set; } = "User";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}