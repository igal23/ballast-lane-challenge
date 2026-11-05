using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Npgsql;
using TaskBoard.Infrastructure.Persistence;

namespace TaskBoard.IntegrationTests.Support;

public sealed class TaskBoardApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:TaskBoard"] = connectionString
            });
        });
    }

    public async Task InitializeDatabaseAsync(CancellationToken cancellationToken)
    {
        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        await new DatabaseMigrator(dataSource).MigrateAsync(cancellationToken);
    }
}
