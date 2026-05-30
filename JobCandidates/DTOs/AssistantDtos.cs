namespace JobCandidates.DTOs
{
    public class GenerateQuestionsRequestDto
    {
        public string JobTitle { get; set; } = string.Empty;
        public string JobDescription { get; set; } = string.Empty;
        public string RequiredSkills { get; set; } = string.Empty;
        public string CandidateName { get; set; } = string.Empty;
        public string CandidateSkills { get; set; } = string.Empty;
        public int ExperienceYears { get; set; }
    }

    public class ExplainRankingRequestDto
    {
        public string JobTitle { get; set; } = string.Empty;
        public string CandidateName { get; set; } = string.Empty;
        public int MatchedSkillCount { get; set; }
        public int TotalRequiredSkills { get; set; }
        public int ExperienceYears { get; set; }
        public double SemanticScore { get; set; }
        public double CombinedScore { get; set; }
    }

    public class EmailTemplateRequestDto
    {
        public string CandidateName { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public string EmailType { get; set; } = string.Empty; // invite, rejection, follow-up
        public string OptionalNotes { get; set; } = string.Empty;
    }

    public class AssistantResponseDto
    {
        public string Content { get; set; } = string.Empty;
    }
}