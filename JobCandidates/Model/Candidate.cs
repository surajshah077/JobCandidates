using System.ComponentModel.DataAnnotations;

namespace JobCandidates.Model
{
    public class Candidate
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Education { get; set; } = string.Empty;

        [Range(0, 50, ErrorMessage = "Experience years must be between 0 and 50.")]
        public int ExperienceYears { get; set; }

        [MaxLength(500)]
        public string Skills { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public List<Application>? Applications { get; set; }
    }
}