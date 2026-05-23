using System.ComponentModel.DataAnnotations;

namespace JobCandidates.DTOs
{
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

    public class RegisterAccountDTO
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Range(18, 80)]
        public int Age { get; set; }

        [Required]
        [MaxLength(30)]
        public string Gender { get; set; } = "PreferNotToSay";

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Role { get; set; } = "User";
    }

    public class SetUserRoleDTO
    {
        [Required]
        public string Role { get; set; } = "User";
    }
}