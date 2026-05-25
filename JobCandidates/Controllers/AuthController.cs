using JobCandidates.DTOs;
using JobCandidates.Model;
using JobCandidates.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace JobCandidates.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailService _emailService;

        public AuthController(ApplicationDbContext db, IEmailService emailService)
        {
            _db = db;
            _emailService = emailService;
        }

        private async Task SignInUserAsync(AppUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Name ?? ""),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role ?? "User")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                });
        }

        [AllowAnonymous]
        [HttpGet("unauthorized")]
        public IActionResult UnauthorizedEndpoint()
        {
            return Unauthorized(new { message = "You must login first." });
        }

        [AllowAnonymous]
        [HttpGet("forbidden")]
        public IActionResult ForbiddenEndpoint()
        {
            return StatusCode(403, new { message = "You are not allowed to access this resource." });
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<ActionResult> Register(RegisterAccountDTO dto)
        {
            var existingUser = await _db.Users.SingleOrDefaultAsync(u => u.Email == dto.Email);
            if (existingUser != null)
            {
                return BadRequest(new ApiError
                {
                    Code = "AccountExists",
                    Message = "Account already exists. Please login."
                });
            }

            var oldPending = await _db.PendingRegistrations
                .Where(x => x.Email == dto.Email && !x.Used)
                .ToListAsync();

            if (oldPending.Any())
            {
                _db.PendingRegistrations.RemoveRange(oldPending);
                await _db.SaveChangesAsync();
            }

            var normalizedRole = dto.Role == "Recruiter" ? "Recruiter" : "User";
            var code = new Random().Next(100000, 999999).ToString();

            var pending = new PendingRegistration
            {
                Name = dto.Name,
                Age = dto.Age,
                Gender = dto.Gender,
                Email = dto.Email,
                Role = normalizedRole,
                OtpCode = code,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                Used = false
            };

            _db.PendingRegistrations.Add(pending);
            await _db.SaveChangesAsync();

            await _emailService.SendOtpEmailAsync(dto.Email, code);

            return Ok(new
            {
                message = "OTP sent to your email. Account will be created after verification."
            });
        }

        [AllowAnonymous]
        [HttpPost("verify-register-otp")]
        public async Task<ActionResult> VerifyRegisterOtp(OtpVerifyDTO dto)
        {
            var pending = await _db.PendingRegistrations
                .Where(x => x.Email == dto.Email &&
                            x.OtpCode == dto.Code &&
                            !x.Used &&
                            x.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            if (pending == null)
            {
                return Unauthorized(new ApiError
                {
                    Code = "InvalidOtp",
                    Message = "Registration OTP is invalid or expired."
                });
            }

            var existingUser = await _db.Users.SingleOrDefaultAsync(u => u.Email == dto.Email);
            if (existingUser != null)
            {
                return BadRequest(new ApiError
                {
                    Code = "AccountExists",
                    Message = "Account already exists."
                });
            }

            var user = new AppUser
            {
                Name = pending.Name,
                Age = pending.Age,
                Gender = pending.Gender,
                Email = pending.Email,
                Role = pending.Role,
                IsEmailVerified = true
            };

            pending.Used = true;
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            await SignInUserAsync(user);

            return Ok(new
            {
                message = "Account verified and logged in successfully."
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

            if (!user.IsEmailVerified)
            {
                return BadRequest(new ApiError
                {
                    Code = "EmailNotVerified",
                    Message = "Email is not verified yet."
                });
            }

            var oldOtps = await _db.OtpCodes
                .Where(x => x.Email == dto.Email && !x.Used)
                .ToListAsync();

            if (oldOtps.Any())
            {
                _db.OtpCodes.RemoveRange(oldOtps);
                await _db.SaveChangesAsync();
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
        [HttpPost("verify-login-otp")]
        public async Task<ActionResult> VerifyLoginOtp(OtpVerifyDTO dto)
        {
            var otp = await _db.OtpCodes
                .Where(o => o.Email == dto.Email &&
                            o.Code == dto.Code &&
                            !o.Used &&
                            o.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(o => o.Id)
                .FirstOrDefaultAsync();

            if (otp == null)
            {
                return Unauthorized(new ApiError
                {
                    Code = "InvalidOtp",
                    Message = "Login OTP is invalid or expired."
                });
            }

            var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
            {
                return NotFound(new ApiError
                {
                    Code = "UserNotFound",
                    Message = "User account not found."
                });
            }

            if (!user.IsEmailVerified)
            {
                return BadRequest(new ApiError
                {
                    Code = "EmailNotVerified",
                    Message = "Email is not verified yet."
                });
            }

            otp.Used = true;
            await _db.SaveChangesAsync();

            await SignInUserAsync(user);

            return Ok(new
            {
                message = "Logged in successfully."
            });
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult Me()
        {
            return Ok(new
            {
                authenticated = true,
                email = User.FindFirstValue(ClaimTypes.Email),
                name = User.FindFirstValue(ClaimTypes.Name),
                role = User.FindFirstValue(ClaimTypes.Role)
            });
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok(new { message = "Logged out successfully." });
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

            var normalizedRole = dto.Role == "Recruiter" || dto.Role == "Admin" ? dto.Role : "User";
            user.Role = normalizedRole;
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}