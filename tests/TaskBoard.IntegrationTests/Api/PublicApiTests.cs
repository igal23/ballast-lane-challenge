using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TaskBoard.IntegrationTests.Support;

namespace TaskBoard.IntegrationTests.Api;

public sealed class PublicApiTests : IClassFixture<PostgresFixture>
{
    private readonly HttpClient _client;
    private readonly TaskBoardApiFactory _factory;

    public PublicApiTests(PostgresFixture postgres)
    {
        _factory = new TaskBoardApiFactory(postgres.ConnectionString);
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Ping_ReturnsOkWithoutAuthentication()
    {
        var response = await _client.GetAsync("/api/public/ping");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("ok", (await response.Content.ReadFromJsonAsync<PingResponse>())!.Status);
    }

    [Fact]
    public void Infrastructure_IsRegisteredOnce()
    {
        Assert.Same(
            _factory.Services.GetRequiredService<NpgsqlDataSource>(),
            _factory.Services.GetRequiredService<NpgsqlDataSource>());
    }

    private sealed record PingResponse(string Status);
}
