using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using TaskBoard.IntegrationTests.Support;

namespace TaskBoard.IntegrationTests.Api;

public sealed class DevelopmentStartupTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _postgres;
    public DevelopmentStartupTests(PostgresFixture postgres) => _postgres = postgres;

    [Fact]
    public async Task DevelopmentConfiguration_StartsPublicApiWithoutExtraJwtSetup()
    {
        await using var baseFactory = new WebApplicationFactory<Program>();
        await using var factory = baseFactory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:TaskBoard"] = _postgres.ConnectionString
                })));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/public/ping");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
