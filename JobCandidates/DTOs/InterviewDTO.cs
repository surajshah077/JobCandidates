using System.ComponentModel.DataAnnotations;
using JobCandidates.Model;

namespace JobCandidates.DTOs
{
    public class CreateInterviewDTO
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "ApplicationId must be a positive integer.")]
        public int ApplicationId { get; set; }

        [Required]
        public DateTime ScheduledDate { get; set; }

        [MaxLength(150)]
        public string InterviewerName { get; set; } = string.Empty;

        [MaxLength(300)]
        public string LocationOrLink { get; set; } = string.Empty;
    }

    public class UpdateInterviewDTO
    {
        [Required]
        public DateTime ScheduledDate { get; set; }

        [MaxLength(150)]
        public string InterviewerName { get; set; } = string.Empty;

        [MaxLength(300)]
        public string LocationOrLink { get; set; } = string.Empty;

        [Required]
        public InterviewFeedback Feedback { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }
}