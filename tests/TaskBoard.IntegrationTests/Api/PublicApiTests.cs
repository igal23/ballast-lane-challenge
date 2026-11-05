using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace TaskBoard.IntegrationTests.Api;

public sealed class PublicApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public PublicApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
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
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.UseSetting(
                "ConnectionStrings:TaskBoard",
                "Host=localhost;Database=taskboard;Username=taskboard;Password=test"));

        Assert.Same(
            factory.Services.GetRequiredService<NpgsqlDataSource>(),
            factory.Services.GetRequiredService<NpgsqlDataSource>());
    }

    private sealed record PingResponse(string Status);
}
