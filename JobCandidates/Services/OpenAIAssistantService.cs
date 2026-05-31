using JobCandidates.DTOs;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace JobCandidates.Services
{
    public class OpenAIAssistantService : IAssistantService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private const string Model = "gpt-4.1-mini";

        public OpenAIAssistantService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["OpenAI:ApiKey"]
                ?? throw new InvalidOperationException("OpenAI:ApiKey is missing from configuration.");
        }

        public Task<string> GenerateInterviewQuestionsAsync(GenerateQuestionsRequestDto request)
        {
            var uniqueSeed = DateTime.UtcNow.Ticks % 10000;
            string prompt = $"""
You are an expert technical recruiter. Generate 5 UNIQUE and SPECIFIC interview questions.
Seed variation: {uniqueSeed}

Job Title: {request.JobTitle}
Job Description: {request.JobDescription}
Required Skills: {request.RequiredSkills}
Candidate Name: {request.CandidateName}
Candidate Skills: {request.CandidateSkills}
Experience Years: {request.ExperienceYears}

Rules:
- Each question must be tailored to this specific candidate's background vs. the job requirements.
- Identify GAPS between candidate skills and required skills, and probe those gaps.
- Mix: 2 technical deep-dives, 1 behavioral, 1 experience-based, 1 situational.
- DO NOT use generic questions like "tell me about yourself".
- Return only numbered questions.
""";
            return SendPromptAsync(prompt, AssistantTaskType.Questions);
        }

        public Task<string> ExplainRankingAsync(ExplainRankingRequestDto request)
        {
            string prompt = $"""
You are an AI assistant helping explain a candidate ranking in a recruitment system.

Explain in 1 short paragraph why this candidate got this score.

Job Title: {request.JobTitle}
Candidate Name: {request.CandidateName}
Matched Skill Count: {request.MatchedSkillCount}
Total Required Skills: {request.TotalRequiredSkills}
Experience Years: {request.ExperienceYears}
Semantic Score: {request.SemanticScore}
Combined Score: {request.CombinedScore}

Rules:
- Use plain, human-readable language.
- Be concise but informative.
- Mention skills, experience, and semantic relevance.
- Do not use bullet points.
""";
            return SendPromptAsync(prompt, AssistantTaskType.Explanation);
        }

        public Task<string> GenerateEmailTemplateAsync(EmailTemplateRequestDto request)
        {
            string prompt = $"""
You are a professional HR assistant.
Write a concise and professional email template.

Email Type: {request.EmailType}
Candidate Name: {request.CandidateName}
Job Title: {request.JobTitle}
Optional Notes: {request.OptionalNotes}

Rules:
- Make it sound natural and professional.
- If Email Type is invite, invite the candidate for an interview.
- If Email Type is rejection, politely reject the candidate.
- If Email Type is follow-up, write a follow-up email.
- Return only the email subject and body.
""";
            return SendPromptAsync(prompt, AssistantTaskType.Email);
        }

        private async Task<string> SendPromptAsync(string prompt, AssistantTaskType taskType)
        {
            var body = new
            {
                model = Model,
                input = prompt,
                temperature = 0.4
            };

            var json = JsonSerializer.Serialize(body);

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request);
            var responseText = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return ExtractOutputText(responseText);
            }

            return BuildFallback(taskType, response.StatusCode, responseText);
        }

        private static string ExtractOutputText(string responseText)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseText);

                if (doc.RootElement.TryGetProperty("output_text", out var outputText))
                    return outputText.GetString() ?? string.Empty;

                if (doc.RootElement.TryGetProperty("output", out var output) && output.ValueKind == JsonValueKind.Array)
                {
                    var parts = new List<string>();

                    foreach (var item in output.EnumerateArray())
                    {
                        if (!item.TryGetProperty("content", out var contentArr) || contentArr.ValueKind != JsonValueKind.Array)
                            continue;

                        foreach (var contentItem in contentArr.EnumerateArray())
                        {
                            if (contentItem.TryGetProperty("text", out var text))
                                parts.Add(text.GetString() ?? string.Empty);
                        }
                    }

                    return string.Join(Environment.NewLine, parts).Trim();
                }
            }
            catch
            {
            }

            return responseText;
        }

        private static string BuildFallback(AssistantTaskType taskType, HttpStatusCode statusCode, string responseText)
        {
            var is429 = statusCode == HttpStatusCode.TooManyRequests;
            var insufficientQuota = responseText.Contains("insufficient_quota", StringComparison.OrdinalIgnoreCase);

            if (is429 && insufficientQuota)
            {
                return taskType switch
                {
                    AssistantTaskType.Questions => "OpenAI quota is insufficient. Fallback: 1) Explain your recent project. 2) Describe a backend challenge you solved. 3) How do you design REST APIs? 4) How do you handle bugs? 5) What skills are you learning next?",
                    AssistantTaskType.Explanation => "OpenAI quota is insufficient. Fallback: This candidate scored well because their skills, experience, and overall profile match the job requirements closely.",
                    AssistantTaskType.Email => "Subject: Regarding Your Application\n\nDear Candidate,\n\nThank you for your interest in the role. We appreciate your application and will share the next steps shortly.\n\nBest regards,\nRecruitment Team",
                    _ => "OpenAI quota is insufficient.",
                };
            }

            if (is429)
            {
                return taskType switch
                {
                    AssistantTaskType.Questions => "OpenAI rate limit hit. Fallback: 1) Explain your recent project. 2) Describe a backend challenge you solved. 3) How do you design REST APIs? 4) How do you handle bugs? 5) What skills are you learning next?",
                    AssistantTaskType.Explanation => "OpenAI rate limit hit. Fallback: This candidate was ranked using skill match, experience, and semantic similarity.",
                    AssistantTaskType.Email => "Subject: Regarding Your Application\n\nDear Candidate,\n\nThank you for your interest in the role. We appreciate your application and will share the next steps shortly.\n\nBest regards,\nRecruitment Team",
                    _ => "OpenAI rate limit hit.",
                };
            }

            return taskType switch
            {
                AssistantTaskType.Questions => "Fallback: 1) Explain your recent project. 2) Describe a backend challenge you solved. 3) How do you design REST APIs? 4) How do you handle bugs? 5) What skills are you learning next?",
                AssistantTaskType.Explanation => "Fallback: This candidate was ranked using skill match, experience, and semantic similarity.",
                AssistantTaskType.Email => "Subject: Regarding Your Application\n\nDear Candidate,\n\nThank you for your application. We will get back to you soon.\n\nBest regards,\nRecruitment Team",
                _ => "Fallback response.",
            };
        }

        private enum AssistantTaskType
        {
            Questions,
            Explanation,
            Email
        }
    }
}