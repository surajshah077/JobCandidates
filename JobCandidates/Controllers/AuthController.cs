using JobCandidates.DTOs;
using JobCandidates.Model;
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

        public AuthController(ApplicationDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
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
                new Claim(ClaimTypes.Name, user.Name ?? user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("gender", user.Gender ?? "PreferNotToSay"),
                new Claim("age", user.Age.ToString())
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
        [HttpPost("request-otp")]
        public async Task<ActionResult> RequestOtp(OtpRequestDTO dto)
        {
            var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
            {
                user = new AppUser
                {
                    Email = dto.Email,
                    Name = "New User",
                    Age = 18,
                    Gender = "PreferNotToSay",
                    Role = "User"
                };
                _db.Users.Add(user);
                await _db.SaveChangesAsync();
            }

            var code = new Random().Next(100000, 999999).ToString();

            var otp = new OtpCode
            {
                Email = dto.Email,
                Code = code,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                Used = false
            };

            _db.OtpCodes.Add(otp);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "OTP generated (in real app, it would be emailed).",
                code
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

            await _db.SaveChangesAsync();

            var jwt = GenerateJwtToken(user);
            return Ok(new LoginResponseDTO { Token = jwt });
        }

        [Authorize]
        [HttpPost("register-details")]
        public async Task<ActionResult<LoginResponseDTO>> RegisterDetails(RegisterDetailsDTO dto)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrWhiteSpace(email))
            {
                return Unauthorized(new ApiError
                {
                    Code = "InvalidToken",
                    Message = "Email claim not found in token."
                });
            }

            var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                return NotFound(new ApiError
                {
                    Code = "UserNotFound",
                    Message = "User account not found."
                });
            }

            user.Name = dto.Name;
            user.Age = dto.Age;
            user.Gender = dto.Gender;

            // Only allow User or Recruiter from self-registration
            user.Role = dto.Role == "Recruiter" ? "Recruiter" : "User";

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