using JobCandidates.DTOs;
using JobCandidates.Model;
using Microsoft.EntityFrameworkCore;

namespace JobCandidates.Repository
{
    public class RankingService : IRankingService
    {
        private readonly ApplicationDbContext _context;

        // Scoring constants
        private const int SkillPointsEach = 10;  // per matched skill
        private const int ExpPointsPerYear = 5;   // per year, up to cap
        private const int MaxExpYears = 10;  // cap: 10+ yrs = same senior bonus

        public RankingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<CandidateScoreDTO>> GetCandidateScoresForJobAsync(int jobId)
        {
            var job = await _context.Jobs.FindAsync(jobId);
            if (job == null) return new List<CandidateScoreDTO>();

            var requiredSkills = SplitSkills(job.RequiredSkills);
            var candidates = await _context.Candidates.ToListAsync();

            // Maximum possible raw score (used for normalisation)
            int maxSkillScore = requiredSkills.Count * SkillPointsEach;
            int maxExpScore = MaxExpYears * ExpPointsPerYear;
            int maxRawScore = maxSkillScore + maxExpScore;
            if (maxRawScore == 0) maxRawScore = 1; // avoid divide-by-zero

            var result = new List<CandidateScoreDTO>();

            foreach (var candidate in candidates)
            {
                var candidateSkills = SplitSkills(candidate.Skills);

                int matchedSkills = requiredSkills.Count(skill => candidateSkills.Contains(skill));
                int skillScore = matchedSkills * SkillPointsEach;

                // Cap experience: 10+ years all get the same maximum bonus
                int cappedExp = Math.Min(candidate.ExperienceYears, MaxExpYears);
                int expScore = cappedExp * ExpPointsPerYear;

                int rawScore = skillScore + expScore;

                // Normalise to 0–100
                int totalScore = (int)Math.Round((double)rawScore / maxRawScore * 100);

                result.Add(new CandidateScoreDTO
                {
                    CandidateId = candidate.Id,
                    CandidateName = candidate.Name,
                    ExperienceYears = candidate.ExperienceYears,
                    SkillMatchScore = skillScore,
                    ExperienceScore = expScore,
                    TotalScore = totalScore
                });
            }

            return result
                .OrderByDescending(x => x.TotalScore)
                .ThenByDescending(x => x.SkillMatchScore)
                .ToList();
        }

        private static List<string> SplitSkills(string? skills) =>
            string.IsNullOrWhiteSpace(skills)
                ? new List<string>()
                : skills.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(s => s.ToLowerInvariant())
                        .Distinct()
                        .ToList();
    }
}