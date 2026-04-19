using JobCandidates.DTOs;
using Microsoft.EntityFrameworkCore;

namespace JobCandidates.Repository
{
    public class AnalyticsRepository : IAnalyticsRepository
    {
        private readonly ApplicationDbContext _context;

        public AnalyticsRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AnalyticsSummaryDTO> GetSummaryAsync()
        {
            return new AnalyticsSummaryDTO
            {
                TotalJobs = await _context.Jobs.CountAsync(),
                TotalCandidates = await _context.Candidates.CountAsync(),
                TotalApplications = await _context.Applications.CountAsync(),
                TotalInterviews = await _context.Interviews.CountAsync()
            };
        }

        public async Task<List<ApplicationStatusCountDTO>> GetApplicationStatusCountsAsync()
        {
            return await _context.Applications
                .GroupBy(a => a.Status)
                .Select(g => new ApplicationStatusCountDTO
                {
                    Status = g.Key.ToString(),
                    Count = g.Count()
                })
                .ToListAsync();
        }

        public async Task<List<JobApplicationCountDTO>> GetApplicationsPerJobAsync()
        {
            return await _context.Applications
                .Include(a => a.Job)
                .GroupBy(a => new { a.JobId, a.Job!.Title })
                .Select(g => new JobApplicationCountDTO
                {
                    JobId = g.Key.JobId,
                    JobTitle = g.Key.Title,
                    ApplicationCount = g.Count()
                })
                .OrderByDescending(x => x.ApplicationCount)
                .ToListAsync();
        }
    }
}