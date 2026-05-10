using System.ComponentModel.DataAnnotations;

namespace JobCandidates.DTOs
{
    public class CreateCandidateRequestDTO
    {
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
    }

    public class UpdateCandidateRequestDTO
    {
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Phone]
        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Education { get; set; } = string.Empty;

        [Range(0, 50, ErrorMessage = "Experience years must be between 0 and 50.")]
        public int ExperienceYears { get; set; }

        [MaxLength(500)]
        public string Skills { get; set; } = string.Empty;
    }
}