using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using TaskBoard.Infrastructure.Persistence;

namespace TaskBoard.IntegrationTests.Support;

public sealed class TaskBoardApiFactory : WebApplicationFactory<Program>
{
    public const string JwtIssuer = "taskboard-integration-tests";
    public const string JwtAudience = "taskboard-test-client";
    public const string JwtSigningKey = "integration-test-signing-key-at-least-32-bytes";
    private readonly string _connectionString;
    private readonly DateTimeOffset? _fixedUtcNow;

    public TaskBoardApiFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public TaskBoardApiFactory(string connectionString, DateTimeOffset fixedUtcNow)
    {
        _connectionString = connectionString;
        _fixedUtcNow = fixedUtcNow;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:TaskBoard"] = _connectionString,
                ["Jwt:Issuer"] = JwtIssuer,
                ["Jwt:Audience"] = JwtAudience,
                ["Jwt:SigningKey"] = JwtSigningKey,
                ["AllowedOrigins:0"] = "http://localhost:4200",
                ["HttpsRedirection:Enabled"] = "false"
            });
        });
        if (_fixedUtcNow is { } fixedUtcNow)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(new FixedTimeProvider(fixedUtcNow));
            });
        }
    }

    public async Task InitializeDatabaseAsync(CancellationToken cancellationToken)
    {
        await using var dataSource = NpgsqlDataSource.Create(_connectionString);
        await new DatabaseMigrator(dataSource).MigrateAsync(cancellationToken);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
