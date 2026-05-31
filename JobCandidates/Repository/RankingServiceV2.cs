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

        // Global weights for hybrid ranking
        private const double RuleWeight = 0.50;
        private const double SemanticWeight = 0.50;

        // Assumptions for normalisation
        private const int MaxExperienceYearsForScore = 10; // cap for scoring
        private const int MaxLocationScore = 20;           // max location bonus (perfect match)
        private const int SkillWeight = 10;                // per skill
        private const int ExperienceWeight = 5;            // per year

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

            // Text representation for embeddings
            string jobText = BuildJobText(job);
            double[] jobEmbedding = await GetEmbeddingSafeAsync(jobText);
            bool semanticEnabled = jobEmbedding.Length > 0;

            int maxRuleScore = ComputeMaxRuleScore(requiredSkills.Count);

            var results = new List<CandidateRankV2DTO>();

            foreach (var candidate in candidates)
            {
                var candidateSkills = SplitSkills(candidate.Skills);

                // ── Rule-based components ─────────────────────────────────────
                int matchedSkills = requiredSkills.Count(skill => candidateSkills.Contains(skill));

                int skillScore = matchedSkills * SkillWeight;

                int effectiveExperienceYears = Math.Min(candidate.ExperienceYears, MaxExperienceYearsForScore);
                int experienceScore = effectiveExperienceYears * ExperienceWeight;

                bool locationMatch;
                int locationScore = ComputeLocationScore(job.Location, candidate.Location, out locationMatch);

                int ruleScore = skillScore + experienceScore + locationScore;

                double normalisedRule = maxRuleScore == 0
                    ? 0
                    : Math.Min((double)ruleScore / maxRuleScore, 1.0);

                // ── Semantic similarity via embeddings ───────────────────────
                double semanticSimilarity = 0;
                if (semanticEnabled)
                {
                    string candidateText = BuildCandidateText(candidate);
                    double[] candidateEmbedding = await GetEmbeddingSafeAsync(candidateText);

                    if (candidateEmbedding.Length == jobEmbedding.Length && candidateEmbedding.Length > 0)
                        semanticSimilarity = CosineSimilarity(jobEmbedding, candidateEmbedding);
                }

                // ── Final hybrid score ───────────────────────────────────────
                double combined = semanticEnabled
                    ? ((normalisedRule * RuleWeight) + (semanticSimilarity * SemanticWeight)) * 100
                    : normalisedRule * 100;

                // ── Explanation ──────────────────────────────────────────────
                string explanation = BuildExplanation(
                    candidateName: candidate.Name,
                    matched: matchedSkills,
                    total: requiredSkills.Count,
                    experience: candidate.ExperienceYears,
                    semantic: semanticSimilarity,
                    combined: combined,
                    semanticEnabled: semanticEnabled,
                    jobLocation: job.Location,
                    candidateLocation: candidate.Location,
                    locationMatch: locationMatch);

                results.Add(new CandidateRankV2DTO
                {
                    CandidateId = candidate.Id,
                    CandidateName = candidate.Name,
                    Email = candidate.Email,
                    ExperienceYears = candidate.ExperienceYears,
                    Location = candidate.Location,
                    Explanation = explanation,
                    Breakdown = new ScoreBreakdownDTO
                    {
                        SkillScore = skillScore,
                        ExperienceScore = experienceScore,
                        LocationScore = locationScore,
                        RuleBasedScore = ruleScore,
                        SemanticScore = Math.Round(semanticSimilarity, 4),
                        CombinedScore = Math.Round(combined, 2),
                        MatchedSkillCount = matchedSkills,
                        TotalRequiredSkills = requiredSkills.Count,
                        IsLocationMatch = locationMatch
                    }
                });
            }

            var ranked = results
                .OrderByDescending(x => x.Breakdown.CombinedScore)
                .ThenByDescending(x => x.Breakdown.RuleBasedScore)
                .ToList();

            for (int i = 0; i < ranked.Count; i++)
                ranked[i].Rank = i + 1;

            return new RankingV2ResponseDTO
            {
                JobId = job.Id,
                JobTitle = job.Title,
                RequiredSkills = job.RequiredSkills,
                JobLocation = job.Location,
                RankedCandidates = ranked
            };
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        private static int ComputeMaxRuleScore(int requiredSkillCount)
        {
            int maxSkillScore = requiredSkillCount * SkillWeight;
            int maxExperienceScore = MaxExperienceYearsForScore * ExperienceWeight;
            return maxSkillScore + maxExperienceScore + MaxLocationScore;
        }

        private static List<string> SplitSkills(string? skills)
        {
            if (string.IsNullOrWhiteSpace(skills))
                return new List<string>();

            return skills
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => s.ToLowerInvariant())
                .Distinct()
                .ToList();
        }

        private static string BuildJobText(dynamic job)
        {
            var title = job.Title ?? string.Empty;
            var description = job.Description ?? string.Empty;
            var skills = job.RequiredSkills ?? string.Empty;
            var location = job.Location ?? string.Empty;

            return $"{title}. {description}. Required skills: {skills}. Location: {location}.";
        }

        private static string BuildCandidateText(dynamic candidate)
        {
            var name = candidate.Name ?? string.Empty;
            var skills = candidate.Skills ?? string.Empty;
            var location = candidate.Location ?? string.Empty;

            return $"{name}. Skills: {skills}. Experience: {candidate.ExperienceYears} years. Location: {location}.";
        }

        private async Task<double[]> GetEmbeddingSafeAsync(string text)
        {
            try
            {
                return await _embeddingService.GetEmbeddingAsync(text);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == (HttpStatusCode)429)
            {
                // simple retry on rate limit
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

        private static int ComputeLocationScore(
            string? jobLocation,
            string? candidateLocation,
            out bool isLocationMatch)
        {
            isLocationMatch = false;

            if (string.IsNullOrWhiteSpace(jobLocation) || string.IsNullOrWhiteSpace(candidateLocation))
                return 0;

            var jobLoc = jobLocation.Trim().ToLowerInvariant();
            var candLoc = candidateLocation.Trim().ToLowerInvariant();

            // simple containment check; you can later replace with geo-distance
            if (candLoc.Contains(jobLoc) || jobLoc.Contains(candLoc))
            {
                isLocationMatch = true;
                return MaxLocationScore;
            }

            return 0;
        }

        private static string BuildExplanation(
            string candidateName,
            int matched,
            int total,
            int experience,
            double semantic,
            double combined,
            bool semanticEnabled,
            string jobLocation,
            string candidateLocation,
            bool locationMatch)
        {
            string skillLine = total == 0
                ? "No specific skills were required for this job."
                : $"{candidateName} matches {matched} out of {total} required skills.";

            string expLine = experience switch
            {
                >= 5 => $"With {experience} years of experience, they bring strong seniority for this role.",
                >= 2 => $"They have {experience} years of experience, which is appropriate for this role.",
                _ => $"They have {experience} year(s) of experience, which is more junior for this position."
            };

            string locationLine;
            if (string.IsNullOrWhiteSpace(jobLocation))
            {
                locationLine = "The job location is not specified, so location did not influence the score.";
            }
            else if (string.IsNullOrWhiteSpace(candidateLocation))
            {
                locationLine = $"The role is based in {jobLocation}, but the candidate location is not provided, so location was not used in scoring.";
            }
            else if (locationMatch)
            {
                locationLine = $"The role is based in {jobLocation}, and the candidate's location {candidateLocation} is considered a good match.";
            }
            else
            {
                locationLine = $"The role is based in {jobLocation}, while the candidate is in {candidateLocation}, so there is no location bonus.";
            }

            string semanticLine;
            if (!semanticEnabled)
            {
                semanticLine = "Semantic AI matching was unavailable, so the score is based purely on rule-based factors.";
            }
            else
            {
                semanticLine = semantic switch
                {
                    >= 0.75 => "The candidate's overall profile is highly relevant to the job description according to the semantic model.",
                    >= 0.50 => "The candidate's profile shows moderate semantic relevance to this job.",
                    _ => "The candidate's profile has limited semantic alignment with this job description."
                };
            }

            return $"{skillLine} {expLine} {locationLine} {semanticLine} Final combined AI score: {Math.Round(combined, 1)}/100.";
        }
    }
}