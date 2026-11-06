using Microsoft.Extensions.Hosting;

namespace TaskBoard.Infrastructure.Persistence;

public sealed class DatabaseBootstrapHostedService(
    DatabaseMigrator migrator,
    DevelopmentSeeder seeder,
    IHostEnvironment environment) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await migrator.MigrateAsync(cancellationToken);
        if (environment.IsDevelopment())
        {
            await seeder.SeedAsync(cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
