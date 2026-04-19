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
                .Include(i => i.Application)
                .ThenInclude(a => a.Candidate)
                .Include(i => i.Application)
                .ThenInclude(a => a.Job)
                .ToListAsync();
        }

        public async Task<Interview?> GetInterviewByIdAsync(int id)
        {
            return await _context.Interviews
                .Include(i => i.Application)
                .ThenInclude(a => a.Candidate)
                .Include(i => i.Application)
                .ThenInclude(a => a.Job)
                .FirstOrDefaultAsync(i => i.Id == id);
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
            if (existingInterview == null) return null;

            existingInterview.ScheduledDate = interview.ScheduledDate;
            existingInterview.InterviewerName = interview.InterviewerName;
            existingInterview.LocationOrLink = interview.LocationOrLink;
            existingInterview.Feedback = interview.Feedback;
            existingInterview.Notes = interview.Notes;

            await _context.SaveChangesAsync();
            return existingInterview;
        }

        public async Task<bool> DeleteInterviewAsync(int id)
        {
            var interview = await _context.Interviews.FindAsync(id);
            if (interview == null) return false;

            _context.Interviews.Remove(interview);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}