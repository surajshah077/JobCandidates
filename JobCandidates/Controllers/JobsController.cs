using JobCandidates.DTOs;
using JobCandidates.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace JobCandidates.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public JobsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private static JobDTO MapToDto(Job job)
        {
            return new JobDTO
            {
                Id = job.Id,
                Title = job.Title,
                Description = job.Description,
                Location = job.Location,
                SalaryRange = job.SalaryRange,
                RequiredSkills = job.RequiredSkills,
                Status = job.Status,
                PostedByEmail = job.PostedByUser != null ? job.PostedByUser.Email : null,
                PostedByName = job.PostedByUser != null ? job.PostedByUser.Name : null
            };
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<JobDTO>>> GetJobs()
        {
            var jobs = await _context.Jobs
                .Include(j => j.PostedByUser)
                .OrderBy(j => j.Id)
                .ToListAsync();

            var result = jobs.Select(MapToDto).ToList();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<JobDTO>> GetJob(int id)
        {
            var job = await _context.Jobs
                .Include(j => j.PostedByUser)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (job == null)
            {
                return NotFound(new
                {
                    message = $"Job with id {id} was not found."
                });
            }

            return Ok(MapToDto(job));
        }

        [Authorize(Roles = "Admin,Recruiter")]
        [HttpPost]
        public async Task<ActionResult<JobDTO>> CreateJob(CreateJob createJob)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(email))
            {
                return Unauthorized(new
                {
                    message = "Logged in user email not found."
                });
            }

            var currentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (currentUser == null)
            {
                return Unauthorized(new
                {
                    message = "Logged in user not found in database."
                });
            }

            var job = new Job
            {
                Title = createJob.Title,
                Description = createJob.Description,
                Location = createJob.Location,
                SalaryRange = createJob.SalaryRange,
                RequiredSkills = createJob.RequiredSkills,
                Status = "Open",
                PostedByUserId = currentUser.Id
            };

            _context.Jobs.Add(job);
            await _context.SaveChangesAsync();

            job = await _context.Jobs
                .Include(j => j.PostedByUser)
                .FirstAsync(j => j.Id == job.Id);

            var result = MapToDto(job);

            return CreatedAtAction(nameof(GetJob), new { id = job.Id }, result);
        }

        [Authorize(Roles = "Admin,Recruiter")]
        [HttpPut("{id}")]
        public async Task<ActionResult<JobDTO>> UpdateJob(int id, UpdateJob updateJob)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var job = await _context.Jobs
                .Include(j => j.PostedByUser)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (job == null)
            {
                return NotFound(new
                {
                    message = $"Job with id {id} was not found."
                });
            }

            job.Title = updateJob.Title;
            job.Description = updateJob.Description;
            job.Location = updateJob.Location;
            job.SalaryRange = updateJob.SalaryRange;
            job.RequiredSkills = updateJob.RequiredSkills;
            job.Status = updateJob.Status;

            await _context.SaveChangesAsync();

            return Ok(MapToDto(job));
        }

        [Authorize(Roles = "Admin,Recruiter")]
        [HttpPut("{id}/close")]
        public async Task<IActionResult> CloseJob(int id)
        {
            var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == id);
            if (job == null)
            {
                return NotFound(new
                {
                    message = $"Job with id {id} was not found."
                });
            }

            job.Status = "Closed";
            await _context.SaveChangesAsync();

            return Ok(new
            {
                id = job.Id,
                status = job.Status
            });
        }

        [Authorize(Roles = "Admin,Recruiter")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteJob(int id)
        {
            var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == id);
            if (job == null)
            {
                return NotFound(new
                {
                    message = $"Job with id {id} was not found."
                });
            }

            _context.Jobs.Remove(job);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}