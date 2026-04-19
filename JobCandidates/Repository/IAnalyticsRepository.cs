using JobCandidates.DTOs;

namespace JobCandidates.Repository
{
    public interface IAnalyticsRepository
    {
        Task<AnalyticsSummaryDTO> GetSummaryAsync();
        Task<List<ApplicationStatusCountDTO>> GetApplicationStatusCountsAsync();
        Task<List<JobApplicationCountDTO>> GetApplicationsPerJobAsync();
    }
}