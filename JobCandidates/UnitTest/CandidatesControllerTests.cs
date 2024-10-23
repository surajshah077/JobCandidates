using AutoMapper;
using JobCandidates.Controllers;
using JobCandidates.DTOs;
using JobCandidates.Model;
using JobCandidates.Repository;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit; 

namespace JobCandidates.Unit_Test
{
    public class CandidatesControllerTests
    {
        [Fact]
        public async Task UpsertCandidate_ReturnOkResult_WithCreateCandidate()
        {
            var candidateDto = new CandidateDto
            {
                Email = "suraj.shah.developer@gmail.com",
                FirstName = "Suraj",
                LastName = "Shah",
                Comment = "Test task from Sigma Software resolved"
            };

            var candidate = new Candidate
            {
                Email = "suraj.shah.developer@gmail.com",
                FirstName = "Suraj",
                LastName = "Shah",
                Comment = "Test task from Sigma Software resolved"
            };

            var mockMapper = new Mock<IMapper>();
            var mockRepo = new Mock<ICandidateRepository>();

            mockMapper.Setup(m => m.Map<CandidateDto>(It.IsAny<Candidate>()))
                      .Returns(new CandidateDto { Email = candidate.Email });

            var controller = new CandidatesController(mockRepo.Object, mockMapper.Object);
            mockRepo.Setup(repo => repo.Upsert(It.IsAny<Candidate>())).ReturnsAsync(candidate);

            var result = await controller.UpsertCandidate(candidateDto);

          
            var okResult = Assert.IsType<OkObjectResult>(result); 
            var returnedCandidate = Assert.IsType<CandidateDto>(okResult.Value); 
            Assert.Equal(candidate.Email, returnedCandidate.Email); 
        }
    }
}
