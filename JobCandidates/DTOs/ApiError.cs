using System.ComponentModel.DataAnnotations;

namespace JobCandidates.DTOs
{
    public class ApiError
    {
        [Required]
        public string Code { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;
    }
}