using JobCandidates.DTOs;
using JobCandidates.Model;
using JobCandidates.Repository;
using Microsoft.AspNetCore.Mvc;

namespace JobCandidates.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApplicationsController : ControllerBase
    {
        private readonly IApplicationRepository _applicationRepository;
        private readonly ICandidateRepository _candidateRepository;
        private readonly IJobRepository _jobRepository;

        public ApplicationsController(
            IApplicationRepository applicationRepository,
            ICandidateRepository candidateRepository,
            IJobRepository jobRepository)
        {
            _applicationRepository = applicationRepository;
            _candidateRepository = candidateRepository;
            _jobRepository = jobRepository;
        }

        [HttpGet]
        public async Task<ActionResult<List<Application>>> GetApplications()
        {
            var applications = await _applicationRepository.GetAllApplicationsAsync();
            return Ok(applications);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Application>> GetApplication(int id)
        {
            var application = await _applicationRepository.GetApplicationByIdAsync(id);
            if (application == null) return NotFound();
            return Ok(application);
        }

        [HttpPost]
        public async Task<ActionResult<Application>> CreateApplication(CreateApplicationDTO dto)
        {
            // 1) Check candidate exists
            var candidate = await _candidateRepository.GetCandidateByIdAsync(dto.CandidateId);
            if (candidate == null)
            {
                return NotFound($"Candidate with id {dto.CandidateId} was not found.");
            }

            // 2) Check job exists
            var job = await _jobRepository.GetJobByIdAsync(dto.JobId);
            if (job == null)
            {
                return NotFound($"Job with id {dto.JobId} was not found.");
            }

            // 3) Prevent applying to closed jobs
            if (job.Status == "Closed")
            {
                return BadRequest("Cannot create an application for a closed job.");
            }

            // 4) Prevent duplicate applications
            var alreadyExists = await _applicationRepository.ApplicationExistsAsync(dto.CandidateId, dto.JobId);
            if (alreadyExists)
            {
                return Conflict("An application for this candidate and job already exists.");
            }

            var application = new Application
            {
                CandidateId = dto.CandidateId,
                JobId = dto.JobId,
                Notes = dto.Notes,
                Status = ApplicationStatus.Applied
            };

            var created = await _applicationRepository.CreateApplicationAsync(application);
            return CreatedAtAction(nameof(GetApplication), new { id = created.Id }, created);
        }

        [HttpPut("{id}/status")]
        public async Task<ActionResult<Application>> UpdateStatus(int id, UpdateApplicationStatusDTO dto)
        {
            var updated = await _applicationRepository.UpdateApplicationStatusAsync(id, dto.Status, dto.Notes);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteApplication(int id)
        {
            var result = await _applicationRepository.DeleteApplicationAsync(id);
            if (!result) return NotFound();
            return NoContent();
        }
    }
}