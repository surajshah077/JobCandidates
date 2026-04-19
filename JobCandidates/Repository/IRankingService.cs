using JobCandidates.DTOs;

namespace JobCandidates.Repository
{
    public interface IRankingService
    {
        Task<List<CandidateScoreDTO>> GetCandidateScoresForJobAsync(int jobId);
    }
}