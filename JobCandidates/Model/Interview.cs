using System.ComponentModel.DataAnnotations;

namespace JobCandidates.Model
{
    public class Interview
    {
        public int Id { get; set; }

        [Required]
        public int ApplicationId { get; set; }

        [Required]
        public DateTime ScheduledDate { get; set; }

        public string InterviewerName { get; set; } = string.Empty;

        public string LocationOrLink { get; set; } = string.Empty;

        public InterviewFeedback Feedback { get; set; } = InterviewFeedback.Pending;

        public string? Notes { get; set; }

        public Application? Application { get; set; }
    }
}