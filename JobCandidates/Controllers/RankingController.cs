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
        private readonly IJobRepository _jobRepository;

        public RankingController(
            IRankingService rankingService,
            IJobRepository jobRepository)
        {
            _rankingService = rankingService;
            _jobRepository = jobRepository;
        }

        [HttpGet("job/{jobId}")]
        public async Task<ActionResult<List<CandidateScoreDTO>>> GetRankedCandidatesForJob(int jobId)
        {
            if (jobId <= 0)
            {
                return BadRequest(new ApiError
                {
                    Code = "InvalidJobId",
                    Message = "jobId must be a positive integer."
                });
            }

            var job = await _jobRepository.GetJobByIdAsync(jobId);
            if (job == null)
            {
                return NotFound(new ApiError
                {
                    Code = "JobNotFound",
                    Message = $"Job with id {jobId} was not found."
                });
            }

            var rankedCandidates = await _rankingService.GetCandidateScoresForJobAsync(jobId);
            return Ok(rankedCandidates);
        }
    }
}