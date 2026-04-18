using JobCandidates.DTOs;
using JobCandidates.Model;
using JobCandidates.Repository;
using Microsoft.AspNetCore.Mvc;

namespace JobCandidates.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CandidatesController : ControllerBase
    {
        private readonly ICandidateRepository _candidateRepository;

        public CandidatesController(ICandidateRepository candidateRepository)
        {
            _candidateRepository = candidateRepository;
        }

        [HttpGet]
        public async Task<ActionResult<List<Candidate>>> GetCandidates()
        {
            var candidates = await _candidateRepository.GetAllCandidatesAsync();
            return Ok(candidates);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Candidate>> GetCandidate(int id)
        {
            var candidate = await _candidateRepository.GetCandidateByIdAsync(id);
            if (candidate == null) return NotFound();
            return Ok(candidate);
        }

        [HttpPost]
        public async Task<ActionResult<Candidate>> CreateCandidate(CreateCandidateDTO dto)
        {
            var candidate = new Candidate
            {
                Name = dto.Name,
                Email = dto.Email,
                Phone = dto.Phone,
                Education = dto.Education,
                ExperienceYears = dto.ExperienceYears,
                Skills = dto.Skills
            };

            var createdCandidate = await _candidateRepository.CreateCandidateAsync(candidate);
            return CreatedAtAction(nameof(GetCandidate), new { id = createdCandidate.Id }, createdCandidate);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Candidate>> UpdateCandidate(int id, UpdateCandidateDTO dto)
        {
            var candidate = new Candidate
            {
                Id = id,
                Name = dto.Name,
                Phone = dto.Phone,
                Education = dto.Education,
                ExperienceYears = dto.ExperienceYears,
                Skills = dto.Skills
            };

            var updatedCandidate = await _candidateRepository.UpdateCandidateAsync(id, candidate);
            if (updatedCandidate == null) return NotFound();
            return Ok(updatedCandidate);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCandidate(int id)
        {
            var result = await _candidateRepository.DeleteCandidateAsync(id);
            if (!result) return NotFound();
            return NoContent();
        }
    }
}