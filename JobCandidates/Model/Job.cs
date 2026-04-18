using System.ComponentModel.DataAnnotations;

namespace JobCandidates.Model
{
    public class Job
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public string SalaryRange { get; set; } = string.Empty;

        public string RequiredSkills { get; set; } = string.Empty; // Comma-separated skills

        public string Status { get; set; } = "Open"; // Open, Closed

        public DateTime PostedDate { get; set; } = DateTime.UtcNow;

        public string PostedBy { get; set; } = string.Empty; // Recruiter email
    }
}