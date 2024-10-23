using JobCandidates.Model;
using Microsoft.EntityFrameworkCore;

namespace JobCandidates.Repository
{
    public class CandidateRepository : ICandidateRepository
    {
        private readonly ApplicationDbContext _context;

        public CandidateRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<Candidate> GetCandidateByEmail(string email)
        {
           return await _context.Candidates.FirstOrDefaultAsync(x => x.Email == email);
        }

        public async Task<Candidate> Upsert(Candidate candidate)
        {
            var existingCandidate = await _context.Candidates.FindAsync(candidate.Email);
            if (existingCandidate != null)
            {
                existingCandidate.FirstName = candidate.FirstName;
                existingCandidate.LastName = candidate.LastName;
                existingCandidate.PhoneNumber = candidate.PhoneNumber;
                existingCandidate.LinkedInUrl = candidate.LinkedInUrl;
                existingCandidate.GitHubUrl = candidate.GitHubUrl;
                existingCandidate.BestTimeToCall = candidate.BestTimeToCall;
                existingCandidate.Comment = candidate.Comment;

                _context.Candidates.Update(existingCandidate);
            }
            else
            {
                await _context.Candidates.AddAsync(candidate);
            }

            await _context.SaveChangesAsync();
            return candidate;
        }
    }
}
