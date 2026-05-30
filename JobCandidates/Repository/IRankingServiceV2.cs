// JobCandidates/Repository/IRankingServiceV2.cs
using JobCandidates.DTOs;

namespace JobCandidates.Repository
{
    public interface IRankingServiceV2
    {
        Task<RankingV2ResponseDTO> GetRankedCandidatesV2Async(int jobId);
    }
}