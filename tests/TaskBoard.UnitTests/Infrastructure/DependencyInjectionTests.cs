using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TaskBoard.Infrastructure;
using TaskBoard.Infrastructure.Persistence;

namespace TaskBoard.UnitTests.Infrastructure;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddTaskBoardInfrastructure_RegistersOneDataSourceAndMigrator()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:TaskBoard"] =
                    "Host=localhost;Database=taskboard;Username=taskboard;Password=test"
            })
            .Build();
        var services = new ServiceCollection();

        services.AddTaskBoardInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();

        Assert.Same(
            provider.GetRequiredService<NpgsqlDataSource>(),
            provider.GetRequiredService<NpgsqlDataSource>());
        Assert.NotNull(provider.GetRequiredService<DatabaseMigrator>());
    }
}
