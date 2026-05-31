//using JobCandidates;
//using JobCandidates.Model;
//using JobCandidates.Repository;
//using Microsoft.EntityFrameworkCore;
//using Xunit;

//public class RankingServiceTests
//{
//    private static ApplicationDbContext CreateDb()
//    {
//        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
//            .UseInMemoryDatabase(Guid.NewGuid().ToString())
//            .Options;

//        var db = new ApplicationDbContext(options);

//        db.Jobs.Add(new Job
//        {
//            Id = 1,
//            Title = "Backend Developer",
//            Description = "Build APIs",
//            Location = "Berlin",
//            RequiredSkills = "C#, SQL",
//            Status = "Open"
//        });

//        db.Candidates.AddRange(
//            new Candidate
//            {
//                Id = 1,
//                Name = "Alice",
//                Email = "a@test.com",
//                ExperienceYears = 5,
//                Skills = "C#, SQL",
//                Location = "Berlin"
//            },
//            new Candidate
//            {
//                Id = 2,
//                Name = "Bob",
//                Email = "b@test.com",
//                ExperienceYears = 1,
//                Skills = "Java",
//                Location = "Munich"
//            }
//        );

//        db.SaveChanges();
//        return db;
//    }

//    [Fact]
//    public async Task RankingService_ReturnsCandidateSortedByScore()
//    {
//        await using var db = CreateDb();
//        var svc = new RankingService(db);

//        var result = await svc.GetCandidateScoresForJobAsync(1);

//        Assert.Equal(1, result[0].CandidateId);
//        Assert.True(result[0].TotalScore > result[1].TotalScore);
//    }
//}