//using System.Net;
//using Xunit;

//public class RankingApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
//{
//    private readonly HttpClient _client;

//    public RankingApiIntegrationTests(CustomWebApplicationFactory factory)
//    {
//        _client = factory.CreateClient();
//    }

//    [Fact]
//    public async Task GetRankingV1_ReturnsOk()
//    {
//        var response = await _client.GetAsync("/api/ranking/job/1");
//        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
//    }

//    [Fact]
//    public async Task GetRankingV2_ReturnsOk()
//    {
//        var response = await _client.GetAsync("/api/ranking/v2/1");
//        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
//    }
//}