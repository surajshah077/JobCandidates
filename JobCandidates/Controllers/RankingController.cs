// JobCandidates/Controllers/RankingController.cs
using JobCandidates.DTOs;
using JobCandidates.Repository;
using Microsoft.AspNetCore.Mvc;

namespace JobCandidates.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RankingController : ControllerBase
    {
        private readonly IRankingService _rankingService;
        private readonly IRankingServiceV2 _rankingServiceV2;
        private readonly IJobRepository _jobRepository;

        public RankingController(
            IRankingService rankingService,
            IRankingServiceV2 rankingServiceV2,
            IJobRepository jobRepository)
        {
            _rankingService = rankingService;
            _rankingServiceV2 = rankingServiceV2;
            _jobRepository = jobRepository;
        }

        // ── V1: original rule-based ranking (unchanged) ──────────────────────
        [HttpGet("job/{jobId}")]
        public async Task<ActionResult<List<CandidateScoreDTO>>> GetRankedCandidatesForJob(int jobId)
        {
            if (jobId <= 0)
                return BadRequest(new ApiError { Code = "InvalidJobId", Message = "jobId must be a positive integer." });

            var job = await _jobRepository.GetJobByIdAsync(jobId);
            if (job == null)
                return NotFound(new ApiError { Code = "JobNotFound", Message = $"Job with id {jobId} was not found." });

            var rankedCandidates = await _rankingService.GetCandidateScoresForJobAsync(jobId);
            return Ok(rankedCandidates);
        }

        // ── V2: hybrid AI-powered semantic + rule ranking ─────────────────────
        [HttpGet("v2/{jobId}")]
        public async Task<ActionResult<RankingV2ResponseDTO>> GetRankedCandidatesV2(int jobId)
        {
            if (jobId <= 0)
                return BadRequest(new ApiError { Code = "InvalidJobId", Message = "jobId must be a positive integer." });

            var job = await _jobRepository.GetJobByIdAsync(jobId);
            if (job == null)
                return NotFound(new ApiError { Code = "JobNotFound", Message = $"Job with id {jobId} was not found." });

            var result = await _rankingServiceV2.GetRankedCandidatesV2Async(jobId);
            return Ok(result);
        }
    }
}