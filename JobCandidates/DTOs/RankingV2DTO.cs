// JobCandidates/DTOs/RankingV2DTO.cs
namespace JobCandidates.DTOs
{
    public class ScoreBreakdownDTO
    {
        // Fine-grained rule-based parts
        public int SkillScore { get; set; }
        public int ExperienceScore { get; set; }
        public int LocationScore { get; set; }

        // Aggregated rule score
        public int RuleBasedScore { get; set; }

        // Semantic similarity (0.0 – 1.0) and combined score (0–100)
        public double SemanticScore { get; set; }
        public double CombinedScore { get; set; }

        // Extra explainability metadata
        public int MatchedSkillCount { get; set; }
        public int TotalRequiredSkills { get; set; }

        // Whether job and candidate locations match
        public bool IsLocationMatch { get; set; }
    }

    public class CandidateRankV2DTO
    {
        public int CandidateId { get; set; }
        public string CandidateName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public int ExperienceYears { get; set; }
        public int Rank { get; set; }   // 1 = best

        // NEW: carry candidate location through to UI
        public string Location { get; set; } = string.Empty;

        // Human-readable explanation for this candidate
        public string Explanation { get; set; } = string.Empty;

        public ScoreBreakdownDTO Breakdown { get; set; } = new();
    }

    public class RankingV2ResponseDTO
    {
        public int JobId { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public string RequiredSkills { get; set; } = string.Empty;
        public string JobLocation { get; set; } = string.Empty;  // helpful for explainability

        public List<CandidateRankV2DTO> RankedCandidates { get; set; } = new();
    }
}