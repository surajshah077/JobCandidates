using System.ComponentModel.DataAnnotations;
using JobCandidates.Model;

namespace JobCandidates.DTOs
{
    public class CreateApplicationDTO
    {
        [Required]
        public int CandidateId { get; set; }

        [Required]
        public int JobId { get; set; }

        public string? Notes { get; set; }
    }

    public class UpdateApplicationStatusDTO
    {
        [Required]
        public ApplicationStatus Status { get; set; }

        public string? Notes { get; set; }
    }
}