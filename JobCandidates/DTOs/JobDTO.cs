using System.ComponentModel.DataAnnotations;

namespace JobCandidates.DTOs
{
    public class JobDTO
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Location { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? SalaryRange { get; set; }

        [Required]
        [MaxLength(500)]
        public string RequiredSkills { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = string.Empty;

        public string? PostedByEmail { get; set; }
        public string? PostedByName { get; set; }
    }

    public class CreateJob
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Location { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? SalaryRange { get; set; }

        [Required]
        [MaxLength(500)]
        public string RequiredSkills { get; set; } = string.Empty;
    }

    public class UpdateJob
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Location { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? SalaryRange { get; set; }

        [Required]
        [MaxLength(500)]
        public string RequiredSkills { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = string.Empty;
    }
}