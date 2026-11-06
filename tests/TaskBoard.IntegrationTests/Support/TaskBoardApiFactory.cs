using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Npgsql;
using TaskBoard.Infrastructure.Persistence;

namespace TaskBoard.IntegrationTests.Support;

public sealed class TaskBoardApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    public const string JwtIssuer = "taskboard-integration-tests";
    public const string JwtAudience = "taskboard-test-client";
    public const string JwtSigningKey = "integration-test-signing-key-at-least-32-bytes";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:TaskBoard"] = connectionString,
                ["Jwt:Issuer"] = JwtIssuer,
                ["Jwt:Audience"] = JwtAudience,
                ["Jwt:SigningKey"] = JwtSigningKey
            });
        });
    }

    public async Task InitializeDatabaseAsync(CancellationToken cancellationToken)
    {
        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        await new DatabaseMigrator(dataSource).MigrateAsync(cancellationToken);
    }
}
