using System.ComponentModel.DataAnnotations;

namespace JobCandidates.DTOs
{
    public class GoogleLoginRequestDTO
    {
        [Required]
        public string IdToken { get; set; } = string.Empty;
    }

    public class LoginResponseDTO
    {
        [Required]
        public string Token { get; set; } = string.Empty;
    }

    public class OtpRequestDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public class OtpVerifyDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Code { get; set; } = string.Empty;
    }
}