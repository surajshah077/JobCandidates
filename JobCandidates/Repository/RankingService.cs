using JobCandidates.DTOs;
using JobCandidates.Model;
using Microsoft.EntityFrameworkCore;

namespace JobCandidates.Repository
{
    public class RankingService : IRankingService
    {
        private readonly ApplicationDbContext _context;

        public RankingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<CandidateScoreDTO>> GetCandidateScoresForJobAsync(int jobId)
        {
            var job = await _context.Jobs.FindAsync(jobId);
            if (job == null) return new List<CandidateScoreDTO>();

            var requiredSkills = job.RequiredSkills
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => s.ToLower())
                .ToList();

            var candidates = await _context.Candidates.ToListAsync();

            var result = new List<CandidateScoreDTO>();

            foreach (var candidate in candidates)
            {
                var candidateSkills = candidate.Skills
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(s => s.ToLower())
                    .ToList();

                int matchedSkills = requiredSkills.Count(skill => candidateSkills.Contains(skill));
                int skillScore = matchedSkills * 10;
                int experienceScore = candidate.ExperienceYears * 5;
                int totalScore = skillScore + experienceScore;

                result.Add(new CandidateScoreDTO
                {
                    CandidateId = candidate.Id,
                    CandidateName = candidate.Name,
                    ExperienceYears = candidate.ExperienceYears,
                    SkillMatchScore = skillScore,
                    TotalScore = totalScore
                });
            }

            return result
                .OrderByDescending(x => x.TotalScore)
                .ToList();
        }
    }
}