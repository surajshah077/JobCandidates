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

        public ApplicationsController(IApplicationRepository applicationRepository)
        {
            _applicationRepository = applicationRepository;
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
            var application = new Application
            {
                CandidateId = dto.CandidateId,
                JobId = dto.JobId,
                Notes = dto.Notes
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