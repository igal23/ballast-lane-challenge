using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TaskBoard.IntegrationTests.Api;

public sealed class DevelopmentStartupTests
{
    [Fact]
    public async Task DevelopmentConfiguration_StartsPublicApiWithoutExtraJwtSetup()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/public/ping");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
