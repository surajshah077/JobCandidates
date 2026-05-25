using System.ComponentModel.DataAnnotations;

namespace JobCandidates.Model
{
    public class Interview
    {
        public int Id { get; set; }

        [Required]
        public int ApplicationId { get; set; }

        public Application? Application { get; set; }

        [Required]
        public DateOnly ScheduledDate { get; set; }

        [Required]
        [MaxLength(50)]
        public string Mode { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Feedback { get; set; }
    }
}