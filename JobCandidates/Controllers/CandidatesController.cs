using AutoMapper;
using JobCandidates.DTOs;
using JobCandidates.Model;
using JobCandidates.Repository;
using Microsoft.AspNetCore.Mvc;

namespace JobCandidates.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CandidatesController : ControllerBase
    {
        private readonly ICandidateRepository _repository;
        private readonly IMapper _mappper;

        public CandidatesController(ICandidateRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mappper = mapper;
        }

        [HttpPost]
        public async Task<IActionResult> UpsertCandidate([FromBody] CandidateDto candidateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var candidate = _mappper.Map<Candidate>(candidateDto);
                var updateCandidate = await _repository.Upsert(candidate);

                return Ok(_mappper.Map<CandidateDto>(updateCandidate));
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetCandidateByEmail(string email)
        {
            var candidate = await _repository.GetCandidateByEmail(email);
            if (candidate == null)
            {
                return NotFound();
            }

            return Ok(_mappper.Map<CandidateDto>(candidate));
        }
    }
}
