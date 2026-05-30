using JobCandidates.DTOs;
using JobCandidates.Services;
using Microsoft.AspNetCore.Mvc;

namespace JobCandidates.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AssistantController : ControllerBase
    {
        private readonly IAssistantService _assistantService;

        public AssistantController(IAssistantService assistantService)
        {
            _assistantService = assistantService;
        }

        [HttpPost("generate-questions")]
        public async Task<ActionResult<AssistantResponseDto>> GenerateQuestions([FromBody] GenerateQuestionsRequestDto request)
        {
            if (request == null)
                return BadRequest(new { message = "Request body is required." });

            var content = await _assistantService.GenerateInterviewQuestionsAsync(request);
            return Ok(new AssistantResponseDto { Content = content });
        }

        [HttpPost("explain-ranking")]
        public async Task<ActionResult<AssistantResponseDto>> ExplainRanking([FromBody] ExplainRankingRequestDto request)
        {
            if (request == null)
                return BadRequest(new { message = "Request body is required." });

            var content = await _assistantService.ExplainRankingAsync(request);
            return Ok(new AssistantResponseDto { Content = content });
        }

        [HttpPost("email-template")]
        public async Task<ActionResult<AssistantResponseDto>> EmailTemplate([FromBody] EmailTemplateRequestDto request)
        {
            if (request == null)
                return BadRequest(new { message = "Request body is required." });

            var content = await _assistantService.GenerateEmailTemplateAsync(request);
            return Ok(new AssistantResponseDto { Content = content });
        }
    }
}