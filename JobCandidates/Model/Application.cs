using System.ComponentModel.DataAnnotations;

namespace JobCandidates.Model
{
    public class Application
    {
        public int Id { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int CandidateId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int JobId { get; set; }

        public DateTime ApplicationDate { get; set; } = DateTime.UtcNow;

        [Required]
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Applied;

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public Candidate? Candidate { get; set; }
        public Job? Job { get; set; }
        public List<Interview>? Interviews { get; set; }
    }
}