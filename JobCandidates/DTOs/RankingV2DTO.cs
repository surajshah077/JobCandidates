// JobCandidates/DTOs/RankingV2DTO.cs
namespace JobCandidates.DTOs
{
    public class ScoreBreakdownDTO
    {
        public int RuleBasedScore { get; set; }   // old logic: skill match + experience
        public double SemanticScore { get; set; }   // cosine similarity 0.0 – 1.0
        public double CombinedScore { get; set; }   // final weighted score
        public int MatchedSkillCount { get; set; }
        public int TotalRequiredSkills { get; set; }
    }

    public class CandidateRankV2DTO
    {
        public int CandidateId { get; set; }
        public string CandidateName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int ExperienceYears { get; set; }
        public int Rank { get; set; }   // 1 = best
        public string Explanation { get; set; } = string.Empty;  // human-readable why
        public ScoreBreakdownDTO Breakdown { get; set; } = new();
    }

    public class RankingV2ResponseDTO
    {
        public int JobId { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public string RequiredSkills { get; set; } = string.Empty;
        public List<CandidateRankV2DTO> RankedCandidates { get; set; } = new();
    }
}