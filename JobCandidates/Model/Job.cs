using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobCandidates.Model
{
    public class Job
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Location { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? SalaryRange { get; set; }

        [Required]
        [MaxLength(500)]
        public string RequiredSkills { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Open";

        public int? PostedByUserId { get; set; }

        [ForeignKey(nameof(PostedByUserId))]
        public AppUser? PostedByUser { get; set; }

        public ICollection<Application> Applications { get; set; } = new List<Application>();
    }
}