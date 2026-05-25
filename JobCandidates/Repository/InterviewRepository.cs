using JobCandidates.Model;
using Microsoft.EntityFrameworkCore;

namespace JobCandidates.Repository
{
    public class InterviewRepository : IInterviewRepository
    {
        private readonly ApplicationDbContext _context;

        public InterviewRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Interview>> GetAllInterviewsAsync()
        {
            return await _context.Interviews
                .OrderBy(i => i.Id)
                .ToListAsync();
        }

        public async Task<Interview?> GetInterviewByIdAsync(int id)
        {
            return await _context.Interviews.FindAsync(id);
        }

        public async Task<Interview> CreateInterviewAsync(Interview interview)
        {
            _context.Interviews.Add(interview);
            await _context.SaveChangesAsync();
            return interview;
        }

        public async Task<Interview?> UpdateInterviewAsync(int id, Interview interview)
        {
            var existingInterview = await _context.Interviews.FindAsync(id);
            if (existingInterview == null)
                return null;

            existingInterview.ApplicationId = interview.ApplicationId;
            existingInterview.ScheduledDate = interview.ScheduledDate;
            existingInterview.Mode = interview.Mode;
            existingInterview.Feedback = interview.Feedback;

            await _context.SaveChangesAsync();
            return existingInterview;
        }

        public async Task<bool> DeleteInterviewAsync(int id)
        {
            var interview = await _context.Interviews.FindAsync(id);
            if (interview == null)
                return false;

            _context.Interviews.Remove(interview);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}