using JobCandidates.Model;

namespace JobCandidates.Repository
{
    public interface IApplicationRepository
    {
        Task<List<Application>> GetAllApplicationsAsync();
        Task<Application?> GetApplicationByIdAsync(int id);
        Task<Application> CreateApplicationAsync(Application application);
        Task<Application?> UpdateApplicationStatusAsync(int id, ApplicationStatus status, string? notes);
        Task<bool> DeleteApplicationAsync(int id);

        
        Task<bool> ApplicationExistsAsync(int candidateId, int jobId);

        
        Task<bool> HasApplicationsForCandidateAsync(int candidateId);
        Task<bool> HasApplicationsForJobAsync(int jobId);
    }
}