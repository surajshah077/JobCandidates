using JobCandidates.Model;

namespace JobCandidates.Repository
{
    public interface IInterviewRepository
    {
        Task<List<Interview>> GetAllInterviewsAsync();
        Task<Interview?> GetInterviewByIdAsync(int id);
        Task<Interview> CreateInterviewAsync(Interview interview);
        Task<Interview?> UpdateInterviewAsync(int id, Interview interview);
        Task<bool> DeleteInterviewAsync(int id);
    }
}