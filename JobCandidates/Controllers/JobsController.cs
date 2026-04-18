using Microsoft.AspNetCore.Mvc;
using JobCandidates.DTOs;
using JobCandidates.Repository;
using JobCandidates.Model;

namespace JobCandidates.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobsController : ControllerBase
    {
        private readonly IJobRepository _jobRepository;

        public JobsController(IJobRepository jobRepository)
        {
            _jobRepository = jobRepository;
        }

        [HttpGet]
        public async Task<ActionResult<List<Job>>> GetJobs()
        {
            var jobs = await _jobRepository.GetAllJobsAsync();
            return Ok(jobs);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Job>> GetJob(int id)
        {
            var job = await _jobRepository.GetJobByIdAsync(id);
            if (job == null) return NotFound();
            return Ok(job);
        }

        [HttpPost]
        public async Task<ActionResult<Job>> CreateJob(CreateJobDTO dto)
        {
            var job = new Job
            {
                Title = dto.Title,
                Description = dto.Description,
                Location = dto.Location,
                SalaryRange = dto.SalaryRange,
                RequiredSkills = dto.RequiredSkills,
                PostedBy = "admin@example.com" // Change this later
            };

            var createdJob = await _jobRepository.CreateJobAsync(job);
            return CreatedAtAction(nameof(GetJob), new { id = createdJob.Id }, createdJob);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Job>> UpdateJob(int id, UpdateJobDTO dto)
        {
            var job = new Job
            {
                Id = id,
                Title = dto.Title,
                Description = dto.Description,
                Location = dto.Location,
                SalaryRange = dto.SalaryRange,
                RequiredSkills = dto.RequiredSkills,
                Status = dto.Status
            };

            var updatedJob = await _jobRepository.UpdateJobAsync(id, job);
            if (updatedJob == null) return NotFound();
            return Ok(updatedJob);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteJob(int id)
        {
            var result = await _jobRepository.DeleteJobAsync(id);
            if (!result) return NotFound();
            return NoContent();
        }

        [HttpPut("{id}/close")]
        public async Task<ActionResult<Job>> CloseJob(int id)
        {
            var closedJob = await _jobRepository.CloseJobAsync(id);
            if (closedJob == null) return NotFound();
            return Ok(closedJob);
        }
    }
}