using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobCandidates.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DebugAuthController : ControllerBase
    {
        [AllowAnonymous]
        [HttpGet("public")]
        public IActionResult Public()
        {
            return Ok(new
            {
                message = "Public endpoint works"
            });
        }

        [Authorize]
        [HttpGet("protected")]
        public IActionResult Protected()
        {
            return Ok(new
            {
                message = "Protected endpoint works",
                email = User.Claims.FirstOrDefault(c => c.Type == "email")?.Value,
                role = User.Claims.FirstOrDefault(c => c.Type == "role")?.Value,
                claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList()
            });
        }

        [Authorize(Roles = "Recruiter,Admin")]
        [HttpGet("recruiter-only")]
        public IActionResult RecruiterOnly()
        {
            return Ok(new
            {
                message = "Recruiter/Admin endpoint works",
                email = User.Claims.FirstOrDefault(c => c.Type == "email")?.Value,
                role = User.Claims.FirstOrDefault(c => c.Type == "role")?.Value
            });
        }
    }
}