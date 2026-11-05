using Npgsql;
using TaskBoard.Domain.Users;
using TaskBoard.Infrastructure.Persistence;
using TaskBoard.IntegrationTests.Support;

namespace TaskBoard.IntegrationTests.Persistence;

public sealed class NpgsqlUserRepositoryTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _postgres;

    public NpgsqlUserRepositoryTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    [Fact]
    public async Task AddAndGet_PersistsAndReadsUserByNormalizedEmailAndId()
    {
        await using var dataSource = NpgsqlDataSource.Create(_postgres.ConnectionString);
        await new DatabaseMigrator(dataSource).MigrateAsync(default);
        var repository = new NpgsqlUserRepository(dataSource);
        var user = NewUser();

        var added = await repository.AddAsync(user, default);

        Assert.True(added);
        Assert.Equal(user, await repository.GetByNormalizedEmailAsync(user.NormalizedEmail, default));
        Assert.Equal(user, await repository.GetByIdAsync(user.Id, default));
    }

    [Fact]
    public async Task ConcurrentAdd_SameNormalizedEmail_ExactlyOneSucceeds()
    {
        await using var dataSource = NpgsqlDataSource.Create(_postgres.ConnectionString);
        await new DatabaseMigrator(dataSource).MigrateAsync(default);
        var repository = new NpgsqlUserRepository(dataSource);
        var normalizedEmail = $"concurrent-{Guid.NewGuid():N}@example.com";
        var first = NewUser(normalizedEmail);
        var second = NewUser(normalizedEmail);

        var results = await Task.WhenAll(
            repository.AddAsync(first, default),
            repository.AddAsync(second, default));

        Assert.Equal(1, results.Count(result => result));
        Assert.Equal(1, results.Count(result => !result));
        await using var countCommand = dataSource.CreateCommand(
            "SELECT COUNT(*) FROM users WHERE normalized_email = @normalized_email;");
        countCommand.Parameters.AddWithValue("normalized_email", normalizedEmail);
        Assert.Equal(1L, await countCommand.ExecuteScalarAsync());
    }

    private static UserAccount NewUser(string? normalizedEmail = null)
    {
        normalizedEmail ??= $"user-{Guid.NewGuid():N}@example.com";
        return new UserAccount(Guid.NewGuid(), normalizedEmail, normalizedEmail, "password-hash");
    }
}
