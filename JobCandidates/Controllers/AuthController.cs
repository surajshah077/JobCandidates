using JobCandidates.DTOs;
using JobCandidates.Model;
using JobCandidates.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace JobCandidates.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;

        public AuthController(ApplicationDbContext db, IConfiguration config, IEmailService emailService)
        {
            _db = db;
            _config = config;
            _emailService = emailService;
        }

        private string GenerateJwtToken(AppUser user)
        {
            var jwtSection = _config.GetSection("Jwt");
            var key = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key missing");
            var issuer = jwtSection["Issuer"] ?? "JobCandidatesApi";
            var audience = jwtSection["Audience"] ?? "JobCandidatesApiClient";

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var creds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<ActionResult> Register(RegisterAccountDTO dto)
        {
            var existing = await _db.Users.SingleOrDefaultAsync(u => u.Email == dto.Email);
            if (existing != null)
            {
                return BadRequest(new ApiError
                {
                    Code = "AccountExists",
                    Message = "Account already exists. Please login."
                });
            }

            var normalizedRole = dto.Role == "Recruiter" ? "Recruiter" : "User";

            var user = new AppUser
            {
                Name = dto.Name,
                Age = dto.Age,
                Gender = dto.Gender,
                Email = dto.Email,
                Role = normalizedRole,
                IsEmailVerified = false
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var code = new Random().Next(100000, 999999).ToString();

            _db.OtpCodes.Add(new OtpCode
            {
                Email = dto.Email,
                Code = code,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                Used = false
            });

            await _db.SaveChangesAsync();

            await _emailService.SendOtpEmailAsync(dto.Email, code);

            return Ok(new
            {
                message = "Account created. OTP has been sent to your email address."
            });
        }

        [AllowAnonymous]
        [HttpPost("request-login-otp")]
        public async Task<ActionResult> RequestLoginOtp(OtpRequestDTO dto)
        {
            var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
            {
                return NotFound(new ApiError
                {
                    Code = "UserNotFound",
                    Message = "Account not found. Please create an account first."
                });
            }

            var code = new Random().Next(100000, 999999).ToString();

            _db.OtpCodes.Add(new OtpCode
            {
                Email = dto.Email,
                Code = code,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                Used = false
            });

            await _db.SaveChangesAsync();

            await _emailService.SendOtpEmailAsync(dto.Email, code);

            return Ok(new
            {
                message = "Login OTP has been sent to your email address."
            });
        }

        [AllowAnonymous]
        [HttpPost("verify-otp")]
        public async Task<ActionResult<LoginResponseDTO>> VerifyOtp(OtpVerifyDTO dto)
        {
            var now = DateTime.UtcNow;

            var otp = await _db.OtpCodes
                .Where(o => o.Email == dto.Email && o.Code == dto.Code && !o.Used && o.ExpiresAt > now)
                .OrderByDescending(o => o.Id)
                .FirstOrDefaultAsync();

            if (otp == null)
            {
                return Unauthorized(new ApiError
                {
                    Code = "InvalidOtp",
                    Message = "OTP is invalid or expired."
                });
            }

            otp.Used = true;

            var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
            {
                return NotFound(new ApiError
                {
                    Code = "UserNotFound",
                    Message = "User account not found."
                });
            }

            user.IsEmailVerified = true;
            await _db.SaveChangesAsync();

            var jwt = GenerateJwtToken(user);
            return Ok(new LoginResponseDTO { Token = jwt });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("users")]
        public async Task<ActionResult<List<object>>> GetUsers()
        {
            var users = await _db.Users
                .OrderBy(u => u.Email)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.Name,
                    u.Age,
                    u.Gender,
                    u.Role,
                    u.IsEmailVerified,
                    u.CreatedAt
                })
                .ToListAsync();

            return Ok(users);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("users/{id}/role")]
        public async Task<IActionResult> SetUserRole(int id, SetUserRoleDTO dto)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new ApiError
                {
                    Code = "UserNotFound",
                    Message = $"User with id {id} was not found."
                });
            }

            user.Role = dto.Role;
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}