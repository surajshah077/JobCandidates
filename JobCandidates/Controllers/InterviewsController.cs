using JobCandidates.DTOs;
using JobCandidates.Model;
using JobCandidates.Repository;
using Microsoft.AspNetCore.Mvc;

namespace JobCandidates.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InterviewsController : ControllerBase
    {
        private readonly IInterviewRepository _interviewRepository;

        public InterviewsController(IInterviewRepository interviewRepository)
        {
            _interviewRepository = interviewRepository;
        }

        [HttpGet]
        public async Task<ActionResult<List<Interview>>> GetInterviews()
        {
            var interviews = await _interviewRepository.GetAllInterviewsAsync();
            return Ok(interviews);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Interview>> GetInterview(int id)
        {
            var interview = await _interviewRepository.GetInterviewByIdAsync(id);
            if (interview == null) return NotFound();
            return Ok(interview);
        }

        [HttpPost]
        public async Task<ActionResult<Interview>> CreateInterview(CreateInterviewDTO dto)
        {
            var interview = new Interview
            {
                ApplicationId = dto.ApplicationId,
                ScheduledDate = dto.ScheduledDate,
                InterviewerName = dto.InterviewerName,
                LocationOrLink = dto.LocationOrLink
            };

            var created = await _interviewRepository.CreateInterviewAsync(interview);
            return CreatedAtAction(nameof(GetInterview), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Interview>> UpdateInterview(int id, UpdateInterviewDTO dto)
        {
            var interview = new Interview
            {
                ScheduledDate = dto.ScheduledDate,
                InterviewerName = dto.InterviewerName,
                LocationOrLink = dto.LocationOrLink,
                Feedback = dto.Feedback,
                Notes = dto.Notes
            };

            var updated = await _interviewRepository.UpdateInterviewAsync(id, interview);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInterview(int id)
        {
            var result = await _interviewRepository.DeleteInterviewAsync(id);
            if (!result) return NotFound();
            return NoContent();
        }
    }
}