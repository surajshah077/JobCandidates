using JobCandidates.DTOs;

namespace JobCandidates.Services
{
    public interface IAssistantService
    {
        Task<string> GenerateInterviewQuestionsAsync(GenerateQuestionsRequestDto request);
        Task<string> ExplainRankingAsync(ExplainRankingRequestDto request);
        Task<string> GenerateEmailTemplateAsync(EmailTemplateRequestDto request);
    }
}