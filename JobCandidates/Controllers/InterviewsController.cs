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
        private readonly IApplicationRepository _applicationRepository;
        private readonly IJobRepository _jobRepository;

        public InterviewsController(
            IInterviewRepository interviewRepository,
            IApplicationRepository applicationRepository,
            IJobRepository jobRepository)
        {
            _interviewRepository = interviewRepository;
            _applicationRepository = applicationRepository;
            _jobRepository = jobRepository;
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
            if (interview == null)
            {
                return NotFound(new ApiError
                {
                    Code = "InterviewNotFound",
                    Message = $"Interview with id {id} was not found."
                });
            }

            return Ok(interview);
        }

        [HttpPost]
        public async Task<ActionResult<Interview>> CreateInterview(CreateInterviewDTO dto)
        {
            var application = await _applicationRepository.GetApplicationByIdAsync(dto.ApplicationId);
            if (application == null)
            {
                return NotFound(new ApiError
                {
                    Code = "ApplicationNotFound",
                    Message = $"Application with id {dto.ApplicationId} was not found."
                });
            }

            var job = await _jobRepository.GetJobByIdAsync(application.JobId);
            if (job == null)
            {
                return NotFound(new ApiError
                {
                    Code = "JobNotFound",
                    Message = $"Job with id {application.JobId} was not found."
                });
            }

            if (job.Status == "Closed")
            {
                return BadRequest(new ApiError
                {
                    Code = "JobClosed",
                    Message = "Cannot schedule an interview for a closed job."
                });
            }

            if (dto.ScheduledDate < DateTime.UtcNow.AddDays(-1))
            {
                return BadRequest(new ApiError
                {
                    Code = "InterviewInPast",
                    Message = "ScheduledDate cannot be in the past."
                });
            }

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
            if (updated == null)
            {
                return NotFound(new ApiError
                {
                    Code = "InterviewNotFound",
                    Message = $"Interview with id {id} was not found."
                });
            }

            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInterview(int id)
        {
            var result = await _interviewRepository.DeleteInterviewAsync(id);
            if (!result)
            {
                return NotFound(new ApiError
                {
                    Code = "InterviewNotFound",
                    Message = $"Interview with id {id} was not found."
                });
            }

            return NoContent();
        }
    }
}