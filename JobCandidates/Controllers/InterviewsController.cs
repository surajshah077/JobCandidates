using JobCandidates.DTOs;
using JobCandidates.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobCandidates.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class InterviewsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public InterviewsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private static InterviewDTO MapToDto(Interview interview)
        {
            return new InterviewDTO
            {
                Id = interview.Id,
                ApplicationId = interview.ApplicationId,
                ScheduledDate = interview.ScheduledDate,
                Mode = interview.Mode,
                Feedback = interview.Feedback
            };
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<InterviewDTO>>> GetInterviews()
        {
            var interviews = await _context.Interviews
                .OrderBy(i => i.Id)
                .ToListAsync();

            return Ok(interviews.Select(MapToDto).ToList());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<InterviewDTO>> GetInterview(int id)
        {
            var interview = await _context.Interviews.FindAsync(id);

            if (interview == null)
            {
                return NotFound(new
                {
                    message = $"Interview with id {id} was not found."
                });
            }

            return Ok(MapToDto(interview));
        }

        [HttpPost]
        public async Task<ActionResult<InterviewDTO>> CreateInterview(CreateInterviewDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (dto.ScheduledDate < DateOnly.FromDateTime(DateTime.Today))
            {
                return BadRequest(new
                {
                    message = "ScheduledDate cannot be in the past."
                });
            }

            var application = await _context.Applications.FindAsync(dto.ApplicationId);
            if (application == null)
            {
                return NotFound(new
                {
                    message = $"Application with id {dto.ApplicationId} was not found."
                });
            }

            var interview = new Interview
            {
                ApplicationId = dto.ApplicationId,
                ScheduledDate = dto.ScheduledDate,
                Mode = dto.Mode,
                Feedback = dto.Feedback
            };

            _context.Interviews.Add(interview);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetInterview), new { id = interview.Id }, MapToDto(interview));
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<InterviewDTO>> UpdateInterview(int id, UpdateInterviewDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (dto.ScheduledDate < DateOnly.FromDateTime(DateTime.Today))
            {
                return BadRequest(new
                {
                    message = "ScheduledDate cannot be in the past."
                });
            }

            var interview = await _context.Interviews.FindAsync(id);
            if (interview == null)
            {
                return NotFound(new
                {
                    message = $"Interview with id {id} was not found."
                });
            }

            interview.ScheduledDate = dto.ScheduledDate;
            interview.Mode = dto.Mode;
            interview.Feedback = dto.Feedback;

            await _context.SaveChangesAsync();

            return Ok(MapToDto(interview));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInterview(int id)
        {
            var interview = await _context.Interviews.FindAsync(id);
            if (interview == null)
            {
                return NotFound(new
                {
                    message = $"Interview with id {id} was not found."
                });
            }

            _context.Interviews.Remove(interview);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}