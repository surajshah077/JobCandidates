using System.ComponentModel.DataAnnotations;
using JobCandidates.Model;

namespace JobCandidates.DTOs
{
    public class CreateApplicationDTO
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "CandidateId must be a positive integer.")]
        public int CandidateId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "JobId must be a positive integer.")]
        public int JobId { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }

    public class UpdateApplicationStatusDTO
    {
        [Required]
        public ApplicationStatus Status { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }
}