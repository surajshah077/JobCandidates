using System.ComponentModel.DataAnnotations;

namespace JobCandidates.Model
{
    public class Job
    {
        public int Id { get; set; }

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
        public string RequiredSkills { get; set; } = string.Empty; // Comma-separated skills

        [RegularExpression("Open|Closed", ErrorMessage = "Status must be either 'Open' or 'Closed'.")]
        public string Status { get; set; } = "Open";

        public DateTime PostedDate { get; set; } = DateTime.UtcNow;

        [EmailAddress]
        [MaxLength(200)]
        public string PostedBy { get; set; } = string.Empty; // Recruiter email

        public List<Application>? Applications { get; set; }
    }
}