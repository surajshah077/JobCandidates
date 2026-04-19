using System.ComponentModel.DataAnnotations;

namespace JobCandidates.Model
{
    public class Application
    {
        public int Id { get; set; }

        [Required]
        public int CandidateId { get; set; }

        [Required]
        public int JobId { get; set; }

        public DateTime ApplicationDate { get; set; } = DateTime.UtcNow;

        public ApplicationStatus Status { get; set; } = ApplicationStatus.Applied;

        public string? Notes { get; set; }

        public Candidate? Candidate { get; set; }
        public Job? Job { get; set; }
        public List<Interview>? Interviews { get; set; }
    }
}