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

        public RankingController(IRankingService rankingService)
        {
            _rankingService = rankingService;
        }

        [HttpGet("job/{jobId}")]
        public async Task<ActionResult<List<CandidateScoreDTO>>> GetRankedCandidatesForJob(int jobId)
        {
            var rankedCandidates = await _rankingService.GetCandidateScoresForJobAsync(jobId);
            return Ok(rankedCandidates);
        }
    }
}