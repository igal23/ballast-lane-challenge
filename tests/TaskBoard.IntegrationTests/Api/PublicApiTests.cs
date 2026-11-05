using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TaskBoard.IntegrationTests.Api;

public sealed class PublicApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public PublicApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Ping_ReturnsOkWithoutAuthentication()
    {
        var response = await _client.GetAsync("/api/public/ping");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("ok", (await response.Content.ReadFromJsonAsync<PingResponse>())!.Status);
    }

    private sealed record PingResponse(string Status);
}
