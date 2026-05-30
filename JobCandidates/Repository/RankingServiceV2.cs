using JobCandidates.DTOs;
using JobCandidates.Services;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace JobCandidates.Repository
{
    public class RankingServiceV2 : IRankingServiceV2
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmbeddingService _embeddingService;

        private const double RuleWeight = 0.50;
        private const double SemanticWeight = 0.50;

        public RankingServiceV2(ApplicationDbContext context, IEmbeddingService embeddingService)
        {
            _context = context;
            _embeddingService = embeddingService;
        }

        public async Task<RankingV2ResponseDTO> GetRankedCandidatesV2Async(int jobId)
        {
            var job = await _context.Jobs.FindAsync(jobId);
            if (job == null)
                return new RankingV2ResponseDTO { JobId = jobId };

            var candidates = await _context.Candidates.ToListAsync();
            var requiredSkills = SplitSkills(job.RequiredSkills);

            string jobText = BuildJobText(job);
            double[] jobEmbedding = await GetEmbeddingSafeAsync(jobText);
            bool semanticEnabled = jobEmbedding.Length > 0;

            int maxRuleScore = (requiredSkills.Count * 10) + (10 * 5);
            if (maxRuleScore == 0) maxRuleScore = 1;

            var results = new List<CandidateRankV2DTO>();

            foreach (var candidate in candidates)
            {
                var candidateSkills = SplitSkills(candidate.Skills);

                int matchedSkills = requiredSkills.Count(skill => candidateSkills.Contains(skill));
                int ruleScore = (matchedSkills * 10) + (candidate.ExperienceYears * 5);
                double normalisedRule = Math.Min((double)ruleScore / maxRuleScore, 1.0);

                double semanticSimilarity = 0;
                if (semanticEnabled)
                {
                    string candidateText = BuildCandidateText(candidate);
                    double[] candidateEmbedding = await GetEmbeddingSafeAsync(candidateText);

                    if (candidateEmbedding.Length == jobEmbedding.Length && candidateEmbedding.Length > 0)
                        semanticSimilarity = CosineSimilarity(jobEmbedding, candidateEmbedding);
                }

                double combined = semanticEnabled
                    ? ((normalisedRule * RuleWeight) + (semanticSimilarity * SemanticWeight)) * 100
                    : normalisedRule * 100;

                string explanation = BuildExplanation(
                    candidate.Name,
                    matchedSkills,
                    requiredSkills.Count,
                    candidate.ExperienceYears,
                    semanticSimilarity,
                    combined,
                    semanticEnabled);

                results.Add(new CandidateRankV2DTO
                {
                    CandidateId = candidate.Id,
                    CandidateName = candidate.Name,
                    Email = candidate.Email,
                    ExperienceYears = candidate.ExperienceYears,
                    Explanation = explanation,
                    Breakdown = new ScoreBreakdownDTO
                    {
                        RuleBasedScore = ruleScore,
                        SemanticScore = Math.Round(semanticSimilarity, 4),
                        CombinedScore = Math.Round(combined, 2),
                        MatchedSkillCount = matchedSkills,
                        TotalRequiredSkills = requiredSkills.Count
                    }
                });
            }

            var ranked = results.OrderByDescending(x => x.Breakdown.CombinedScore).ToList();
            for (int i = 0; i < ranked.Count; i++)
                ranked[i].Rank = i + 1;

            return new RankingV2ResponseDTO
            {
                JobId = job.Id,
                JobTitle = job.Title,
                RequiredSkills = job.RequiredSkills,
                RankedCandidates = ranked
            };
        }

        private static List<string> SplitSkills(string? skills)
        {
            if (string.IsNullOrWhiteSpace(skills))
                return new List<string>();

            return skills.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => s.ToLowerInvariant())
                .Distinct()
                .ToList();
        }

        private static string BuildJobText(dynamic job)
        {
            var title = job.Title ?? string.Empty;
            var description = job.Description ?? string.Empty;
            var skills = job.RequiredSkills ?? string.Empty;

            return $"{title}. {description}. Required skills: {skills}";
        }

        private static string BuildCandidateText(dynamic candidate)
        {
            var name = candidate.Name ?? string.Empty;
            var skills = candidate.Skills ?? string.Empty;

            return $"{name}. Skills: {skills}. Experience: {candidate.ExperienceYears} years.";
        }

        private async Task<double[]> GetEmbeddingSafeAsync(string text)
        {
            try
            {
                return await _embeddingService.GetEmbeddingAsync(text);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == (HttpStatusCode)429)
            {
                await Task.Delay(1500);

                try
                {
                    return await _embeddingService.GetEmbeddingAsync(text);
                }
                catch
                {
                    return Array.Empty<double>();
                }
            }
            catch
            {
                return Array.Empty<double>();
            }
        }

        private static double CosineSimilarity(double[] a, double[] b)
        {
            if (a.Length != b.Length || a.Length == 0)
                return 0;

            double dot = 0, normA = 0, normB = 0;

            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                normA += a[i] * a[i];
                normB += b[i] * b[i];
            }

            if (normA == 0 || normB == 0)
                return 0;

            return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
        }

        private static string BuildExplanation(
            string name,
            int matched,
            int total,
            int experience,
            double semantic,
            double combined,
            bool semanticEnabled)
        {
            string skillLine = total == 0
                ? "No specific skills were required for this job."
                : $"{name} matches {matched} out of {total} required skills.";

            string expLine = experience >= 5
                ? $"With {experience} years of experience, they bring strong seniority."
                : experience >= 2
                    ? $"They have {experience} years of experience, suitable for this role."
                    : $"They have {experience} year(s) of experience, which is entry-level for this role.";

            if (!semanticEnabled)
            {
                return $"{skillLine} Semantic AI matching was unavailable, so the score is based on rule-based ranking only. {expLine} Combined score: {Math.Round(combined, 1)}/100.";
            }

            string semanticLine = semantic >= 0.75
                ? "The candidate's profile is highly relevant to the job description."
                : semantic >= 0.50
                    ? "The candidate's profile shows moderate relevance to the job."
                    : "The candidate's profile has limited alignment with this job description.";

            return $"{skillLine} {semanticLine} {expLine} Combined AI score: {Math.Round(combined, 1)}/100.";
        }
    }
}