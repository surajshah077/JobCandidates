using JobCandidates.Model;

namespace JobCandidates.Repository
{
    public interface ICandidateRepository
    {
        Task<List<Candidate>> GetAllCandidatesAsync();
        Task<Candidate?> GetCandidateByIdAsync(int id);
        Task<Candidate> CreateCandidateAsync(Candidate candidate);
        Task<Candidate?> UpdateCandidateAsync(int id, Candidate candidate);
        Task<bool> DeleteCandidateAsync(int id);
    }
}