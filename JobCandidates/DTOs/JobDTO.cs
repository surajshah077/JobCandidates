using System.ComponentModel.DataAnnotations;

namespace JobCandidates.DTOs
{
    public class CreateJobDTO
    {
        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Location { get; set; } = string.Empty;

        [MaxLength(100)]
        public string SalaryRange { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string RequiredSkills { get; set; } = string.Empty;
    }

    public class UpdateJobDTO
    {
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Location { get; set; } = string.Empty;

        [MaxLength(100)]
        public string SalaryRange { get; set; } = string.Empty;

        [MaxLength(500)]
        public string RequiredSkills { get; set; } = string.Empty;

        // Only allow "Open" or "Closed"
        [RegularExpression("Open|Closed", ErrorMessage = "Status must be either 'Open' or 'Closed'.")]
        public string Status { get; set; } = "Open";
    }
}