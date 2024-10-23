using JobCandidates.Model;

namespace JobCandidates.Repository
{
    public interface ICandidateRepository
    {
        Task<Candidate> Upsert(Candidate candidate);
        Task<Candidate> GetCandidateByEmail (string email); 
    }
}
