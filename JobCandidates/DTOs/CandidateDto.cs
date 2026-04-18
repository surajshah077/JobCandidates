using System.ComponentModel.DataAnnotations;

namespace JobCandidates.DTOs
{
    public class CreateCandidateDTO
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string Education { get; set; } = string.Empty;

        public int ExperienceYears { get; set; }

        public string Skills { get; set; } = string.Empty;
    }

    public class UpdateCandidateDTO
    {
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Education { get; set; } = string.Empty;
        public int ExperienceYears { get; set; }
        public string Skills { get; set; } = string.Empty;
       
    }
}