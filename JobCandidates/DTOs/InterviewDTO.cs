using System.ComponentModel.DataAnnotations;
using JobCandidates.Model;

namespace JobCandidates.DTOs
{
    public class CreateInterviewDTO
    {
        [Required]
        public int ApplicationId { get; set; }

        [Required]
        public DateTime ScheduledDate { get; set; }

        public string InterviewerName { get; set; } = string.Empty;

        public string LocationOrLink { get; set; } = string.Empty;
    }

    public class UpdateInterviewDTO
    {
        public DateTime ScheduledDate { get; set; }
        public string InterviewerName { get; set; } = string.Empty;
        public string LocationOrLink { get; set; } = string.Empty;
        public InterviewFeedback Feedback { get; set; }
        public string? Notes { get; set; }
    }
}