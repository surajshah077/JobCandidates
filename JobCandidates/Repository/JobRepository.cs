using JobCandidates.Model;
using Microsoft.EntityFrameworkCore;
using JobCandidates.Repository;

namespace JobCandidates.Repository
{
    public class JobRepository : IJobRepository
    {
        private readonly ApplicationDbContext _context;

        public JobRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Job>> GetAllJobsAsync()
        {
            return await _context.Jobs.ToListAsync();
        }

        public async Task<Job?> GetJobByIdAsync(int id)
        {
            return await _context.Jobs.FindAsync(id);
        }

        public async Task<Job> CreateJobAsync(Job job)
        {
            _context.Jobs.Add(job);
            await _context.SaveChangesAsync();
            return job;
        }

        public async Task<Job?> UpdateJobAsync(int id, Job job)
        {
            var existingJob = await _context.Jobs.FindAsync(id);
            if (existingJob == null) return null;

            existingJob.Title = job.Title;
            existingJob.Description = job.Description;
            existingJob.Location = job.Location;
            existingJob.SalaryRange = job.SalaryRange;
            existingJob.RequiredSkills = job.RequiredSkills;

            await _context.SaveChangesAsync();
            return existingJob;
        }

        public async Task<bool> DeleteJobAsync(int id)
        {
            var job = await _context.Jobs.FindAsync(id);
            if (job == null) return false;

            _context.Jobs.Remove(job);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Job?> CloseJobAsync(int id)
        {
            var job = await _context.Jobs.FindAsync(id);
            if (job == null) return null;

            job.Status = "Closed";
            await _context.SaveChangesAsync();
            return job;
        }
    }
}