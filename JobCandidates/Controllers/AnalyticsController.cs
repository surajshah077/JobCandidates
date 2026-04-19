using JobCandidates.Repository;
using Microsoft.AspNetCore.Mvc;

namespace JobCandidates.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticsRepository _analyticsRepository;

        public AnalyticsController(IAnalyticsRepository analyticsRepository)
        {
            _analyticsRepository = analyticsRepository;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var summary = await _analyticsRepository.GetSummaryAsync();
            return Ok(summary);
        }

        [HttpGet("application-status")]
        public async Task<IActionResult> GetApplicationStatusCounts()
        {
            var counts = await _analyticsRepository.GetApplicationStatusCountsAsync();
            return Ok(counts);
        }

        [HttpGet("applications-per-job")]
        public async Task<IActionResult> GetApplicationsPerJob()
        {
            var counts = await _analyticsRepository.GetApplicationsPerJobAsync();
            return Ok(counts);
        }
    }
}