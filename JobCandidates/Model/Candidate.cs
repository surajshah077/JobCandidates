using System.ComponentModel.DataAnnotations;

namespace JobCandidates.Model
{
    public class Candidate
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string Education { get; set; } = string.Empty;

        public int ExperienceYears { get; set; }

        public string Skills { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public List<Application>? Applications { get; set; }
    }
}