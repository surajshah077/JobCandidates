using System.ComponentModel.DataAnnotations;

namespace JobCandidates.Model
{
    public class Candidate
    {
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }

        public string PhoneNumber { get; set; }
        [Key]
        [Required]
        public string Email { get; set; }
        public string BestTimeToCall { get; set; }
        public string LinkedInUrl { get; set; }
        public string GitHubUrl { get; set; }

        [Required]
        public string Comment { get; set; }
    }
}
