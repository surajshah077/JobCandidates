using System.ComponentModel.DataAnnotations;

namespace JobCandidates.DTOs
{
    public class CreateJobDTO
    {
        [Required]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public string SalaryRange { get; set; } = string.Empty;

        public string RequiredSkills { get; set; } = string.Empty;
    }

    public class UpdateJobDTO
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string SalaryRange { get; set; } = string.Empty;
        public string RequiredSkills { get; set; } = string.Empty;
        public string Status { get; set; } = "Open";
    }
}