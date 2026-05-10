using System.ComponentModel.DataAnnotations;

namespace JobCandidates.Model
{
    public class Interview
    {
        public int Id { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int ApplicationId { get; set; }

        [Required]
        public DateTime ScheduledDate { get; set; }

        [MaxLength(150)]
        public string InterviewerName { get; set; } = string.Empty;

        [MaxLength(300)]
        public string LocationOrLink { get; set; } = string.Empty;

        public InterviewFeedback Feedback { get; set; } = InterviewFeedback.Pending;

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public Application? Application { get; set; }
    }
}