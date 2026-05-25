using System.ComponentModel.DataAnnotations;

namespace JobCandidates.DTOs
{
    public class InterviewDTO
    {
        public int Id { get; set; }
        public int ApplicationId { get; set; }
        public DateOnly ScheduledDate { get; set; }
        public string Mode { get; set; } = string.Empty;
        public string? Feedback { get; set; }
    }
    public class CreateInterviewDTO
    {
        [Required]
        public int ApplicationId { get; set; }

        [Required]
        public DateOnly ScheduledDate { get; set; }

        [Required]
        [MaxLength(50)]
        public string Mode { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Feedback { get; set; }
    }
    public class UpdateInterviewDTO
    {
        [Required]
        public DateOnly ScheduledDate { get; set; }

        [Required]
        [MaxLength(50)]
        public string Mode { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Feedback { get; set; }
    }
}