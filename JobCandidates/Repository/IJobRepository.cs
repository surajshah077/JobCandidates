using JobCandidates.Model;

namespace JobCandidates.Repository
{
    public interface IJobRepository
    {
        Task<List<Job>> GetAllJobsAsync();
        Task<Job?> GetJobByIdAsync(int id);
        Task<Job> CreateJobAsync(Job job);
        Task<Job?> UpdateJobAsync(int id, Job job);
        Task<bool> DeleteJobAsync(int id);
        Task<Job?> CloseJobAsync(int id);
    }
}