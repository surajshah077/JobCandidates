using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobCandidates.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AnalyticsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AnalyticsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAnalytics()
        {
            var totalJobs = await _context.Jobs.CountAsync();
            var totalCandidates = await _context.Candidates.CountAsync();
            var totalApplications = await _context.Applications.CountAsync();
            var totalInterviews = await _context.Interviews.CountAsync();

            var applicationsByStatus = await _context.Applications
                .GroupBy(a => a.Status)
                .Select(g => new
                {
                    status = g.Key,
                    count = g.Count()
                })
                .ToListAsync();

            var jobsByStatus = await _context.Jobs
                .GroupBy(j => j.Status)
                .Select(g => new
                {
                    status = g.Key,
                    count = g.Count()
                })
                .ToListAsync();

            var topJobsByApplications = await _context.Jobs
                .Select(j => new
                {
                    jobId = j.Id,
                    title = j.Title,
                    applicationCount = j.Applications.Count
                })
                .OrderByDescending(x => x.applicationCount)
                .ThenBy(x => x.jobId)
                .Take(10)
                .ToListAsync();

            return Ok(new
            {
                totalJobs,
                totalCandidates,
                totalApplications,
                totalInterviews,
                applicationsByStatus,
                jobsByStatus,
                topJobsByApplications
            });
        }
    }
}