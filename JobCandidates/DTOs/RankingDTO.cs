namespace JobCandidates.DTOs
{
    public class CandidateScoreDTO
    {
        public int CandidateId { get; set; }
        public string CandidateName { get; set; } = string.Empty;
        public int ExperienceYears { get; set; }
        public int SkillMatchScore { get; set; }
        public int TotalScore { get; set; }
    }

    public class RankedCandidateDTO
    {
        public int CandidateId { get; set; }
        public string CandidateName { get; set; } = string.Empty;
        public int TotalScore { get; set; }
    }
}