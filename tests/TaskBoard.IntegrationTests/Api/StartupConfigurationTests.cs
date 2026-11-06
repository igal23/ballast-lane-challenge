using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Npgsql;
using TaskBoard.IntegrationTests.Support;

namespace TaskBoard.IntegrationTests.Api;

public sealed class StartupConfigurationTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _postgres;
    public StartupConfigurationTests(PostgresFixture postgres) => _postgres = postgres;

    [Fact]
    public async Task DevelopmentStartup_MigratesSeedsAndIsIdempotent()
    {
        await ResetDataIfSchemaExists();
        await using (var firstFactory = new TaskBoardApiFactory(_postgres.ConnectionString))
        {
            using var client = firstFactory.CreateClient();
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/public/ping")).StatusCode);
        }

        await using (var secondFactory = new TaskBoardApiFactory(_postgres.ConnectionString))
        {
            using var client = secondFactory.CreateClient();
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/public/ping")).StatusCode);
        }

        Assert.Equal((1L, 3L), await CountDemoData());
    }

    [Fact]
    public async Task ProductionStartup_MigratesWithoutSeedData()
    {
        await ResetDataIfSchemaExists();
        await using var factory = new TaskBoardApiFactory(_postgres.ConnectionString)
            .WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        using var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/public/ping")).StatusCode);
        Assert.Equal((0L, 0L), await CountDemoData());
    }

    [Fact]
    public async Task Cors_AllowsConfiguredOriginAndRejectsOtherOrigins()
    {
        await using var factory = new TaskBoardApiFactory(_postgres.ConnectionString);
        using var client = factory.CreateClient();

        using var allowedRequest = new HttpRequestMessage(HttpMethod.Get, "/api/public/ping");
        allowedRequest.Headers.Add("Origin", "http://localhost:4200");
        var allowed = await client.SendAsync(allowedRequest);
        using var rejectedRequest = new HttpRequestMessage(HttpMethod.Get, "/api/public/ping");
        rejectedRequest.Headers.Add("Origin", "https://evil.example");
        var rejected = await client.SendAsync(rejectedRequest);

        Assert.Equal("http://localhost:4200", Assert.Single(allowed.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.False(rejected.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Theory]
    [InlineData("ConnectionStrings:TaskBoard", "ConnectionStrings:TaskBoard")]
    [InlineData("Jwt:Issuer", "Jwt:Issuer")]
    [InlineData("Jwt:Audience", "Jwt:Audience")]
    [InlineData("Jwt:SigningKey", "Jwt:SigningKey")]
    [InlineData("AllowedOrigins:0", "AllowedOrigins")]
    public void ProductionStartup_MissingRequiredConfiguration_FailsClearly(
        string missingKey,
        string expectedMessage)
    {
        using var baseFactory = new TaskBoardApiFactory(_postgres.ConnectionString);
        using var factory = baseFactory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [missingKey] = string.Empty
                }));
        });

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains(expectedMessage, exception.ToString());
    }

    private async Task<(long Users, long Tasks)> CountDemoData()
    {
        await using var dataSource = NpgsqlDataSource.Create(_postgres.ConnectionString);
        await using var command = dataSource.CreateCommand(
            """
            SELECT
                COUNT(DISTINCT u.id),
                COUNT(t.id)
            FROM users u
            LEFT JOIN tasks t ON t.owner_id = u.id
            WHERE u.normalized_email = 'demo@taskboard.local';
            """);
        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        return (reader.GetInt64(0), reader.GetInt64(1));
    }

    private async Task ResetDataIfSchemaExists()
    {
        await using var dataSource = NpgsqlDataSource.Create(_postgres.ConnectionString);
        await using var command = dataSource.CreateCommand(
            """
            DO $$
            BEGIN
                IF to_regclass('public.tasks') IS NOT NULL THEN
                    TRUNCATE TABLE tasks, users CASCADE;
                END IF;
            END $$;
            """);
        await command.ExecuteNonQueryAsync();
    }
}
