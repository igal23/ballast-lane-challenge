using Npgsql;
using TaskBoard.Infrastructure.Persistence;
using TaskBoard.Infrastructure.Security;
using TaskBoard.IntegrationTests.Support;

namespace TaskBoard.IntegrationTests.Persistence;

public sealed class DevelopmentSeederTests : IClassFixture<PostgresFixture>
{
    private const string DemoPassword = "TaskBoard demo passphrase 2026!";
    private readonly PostgresFixture _postgres;
    public DevelopmentSeederTests(PostgresFixture postgres) => _postgres = postgres;

    [Fact]
    public async Task SeedAsync_Twice_CreatesOneHashedDemoUserAndThreeRepresentativeTasks()
    {
        await using var dataSource = NpgsqlDataSource.Create(_postgres.ConnectionString);
        await new DatabaseMigrator(dataSource).MigrateAsync(default);
        var hasher = new AspNetPasswordHasher();
        var seeder = new DevelopmentSeeder(dataSource, hasher);

        await seeder.SeedAsync(default);
        await seeder.SeedAsync(default);

        await using var command = dataSource.CreateCommand(
            """
            SELECT u.password_hash, COUNT(t.id), array_agg(t.status ORDER BY t.status)
            FROM users u
            LEFT JOIN tasks t ON t.owner_id = u.id
            WHERE u.normalized_email = 'demo@taskboard.local'
            GROUP BY u.password_hash;
            """);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        var hash = reader.GetString(0);
        Assert.NotEqual(DemoPassword, hash);
        Assert.True(hasher.Verify(hash, DemoPassword));
        Assert.Equal(3, reader.GetInt64(1));
        Assert.Equal(["Completed", "Pending", "Wip"], reader.GetFieldValue<string[]>(2));
        Assert.False(await reader.ReadAsync());
    }
}
