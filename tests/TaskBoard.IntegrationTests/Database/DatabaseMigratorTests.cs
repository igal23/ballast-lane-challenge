using Npgsql;
using TaskBoard.Infrastructure.Persistence;
using TaskBoard.IntegrationTests.Support;

namespace TaskBoard.IntegrationTests.Database;

public sealed class DatabaseMigratorTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _postgres;

    public DatabaseMigratorTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    public static TheoryData<string, string?> OversizedTaskText => new()
    {
        { new string('x', 201), null },
        { "Task", new string('x', 2001) }
    };

    [Fact]
    public async Task MigrateAsync_CreatesSchemaOnceAndIsIdempotent()
    {
        await using var dataSource = NpgsqlDataSource.Create(_postgres.ConnectionString);
        var migrator = new DatabaseMigrator(dataSource);

        await migrator.MigrateAsync(default);
        await migrator.MigrateAsync(default);

        Assert.Equal(
            ["schema_versions", "tasks", "users"],
            await ReadApplicationTables(dataSource));
        Assert.Equal(1, await CountAppliedVersions(dataSource));
    }

    [Fact]
    public async Task Schema_RejectsDuplicateNormalizedEmail()
    {
        await using var dataSource = NpgsqlDataSource.Create(_postgres.ConnectionString);
        await new DatabaseMigrator(dataSource).MigrateAsync(default);
        await InsertUser(dataSource, Guid.NewGuid(), "first@example.com", "same@example.com");

        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            InsertUser(dataSource, Guid.NewGuid(), "second@example.com", "same@example.com"));

        Assert.Equal(PostgresErrorCodes.UniqueViolation, exception.SqlState);
        Assert.Equal("uq_users_normalized_email", exception.ConstraintName);
    }

    [Fact]
    public async Task Schema_RejectsEmailLongerThan254Characters()
    {
        await using var dataSource = NpgsqlDataSource.Create(_postgres.ConnectionString);
        await new DatabaseMigrator(dataSource).MigrateAsync(default);

        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            InsertUser(dataSource, Guid.NewGuid(), new string('x', 255), "length@example.com"));

        Assert.Equal(PostgresErrorCodes.StringDataRightTruncation, exception.SqlState);
    }

    [Fact]
    public async Task Schema_RejectsNormalizedEmailLongerThan254Characters()
    {
        await using var dataSource = NpgsqlDataSource.Create(_postgres.ConnectionString);
        await new DatabaseMigrator(dataSource).MigrateAsync(default);

        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            InsertUser(dataSource, Guid.NewGuid(), "length@example.com", new string('x', 255)));

        Assert.Equal(PostgresErrorCodes.StringDataRightTruncation, exception.SqlState);
    }

    [Fact]
    public async Task Schema_RejectsBlankTaskTitle()
    {
        await using var dataSource = NpgsqlDataSource.Create(_postgres.ConnectionString);
        await new DatabaseMigrator(dataSource).MigrateAsync(default);
        var ownerId = Guid.NewGuid();
        await InsertUser(dataSource, ownerId, $"{ownerId}@example.com", $"{ownerId}@example.com");

        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            InsertTask(dataSource, ownerId, "   ", null, "Pending"));

        Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
        Assert.Equal("ck_tasks_title_not_blank", exception.ConstraintName);
    }

    [Theory]
    [MemberData(nameof(OversizedTaskText))]
    public async Task Schema_RejectsTaskTextOverDomainLimits(string title, string? description)
    {
        await using var dataSource = NpgsqlDataSource.Create(_postgres.ConnectionString);
        await new DatabaseMigrator(dataSource).MigrateAsync(default);
        var ownerId = Guid.NewGuid();
        await InsertUser(dataSource, ownerId, $"{ownerId}@example.com", $"{ownerId}@example.com");

        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            InsertTask(dataSource, ownerId, title, description, "Pending"));

        Assert.Equal(PostgresErrorCodes.StringDataRightTruncation, exception.SqlState);
    }

    [Fact]
    public async Task Schema_RejectsInvalidTaskStatus()
    {
        await using var dataSource = NpgsqlDataSource.Create(_postgres.ConnectionString);
        await new DatabaseMigrator(dataSource).MigrateAsync(default);
        var ownerId = Guid.NewGuid();
        await InsertUser(dataSource, ownerId, $"{ownerId}@example.com", $"{ownerId}@example.com");

        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            InsertTask(dataSource, ownerId, "Task", null, "Unknown"));

        Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
        Assert.Equal("ck_tasks_status", exception.ConstraintName);
    }

    [Fact]
    public async Task Schema_RejectsTaskForUnknownOwner()
    {
        await using var dataSource = NpgsqlDataSource.Create(_postgres.ConnectionString);
        await new DatabaseMigrator(dataSource).MigrateAsync(default);

        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            InsertTask(dataSource, Guid.NewGuid(), "Task", null, "Pending"));

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, exception.SqlState);
        Assert.Equal("fk_tasks_owner", exception.ConstraintName);
    }

    [Fact]
    public async Task Schema_CreatesOwnerSortIndex()
    {
        await using var dataSource = NpgsqlDataSource.Create(_postgres.ConnectionString);
        await new DatabaseMigrator(dataSource).MigrateAsync(default);
        await using var command = dataSource.CreateCommand(
            """
            SELECT EXISTS(
                SELECT 1
                FROM pg_indexes
                WHERE schemaname = 'public'
                  AND tablename = 'tasks'
                  AND indexname = 'idx_tasks_owner_sort');
            """);

        Assert.True((bool)(await command.ExecuteScalarAsync())!);
    }

    private static async Task<string[]> ReadApplicationTables(NpgsqlDataSource dataSource)
    {
        await using var command = dataSource.CreateCommand(
            """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'public'
              AND table_type = 'BASE TABLE'
            ORDER BY table_name;
            """);
        await using var reader = await command.ExecuteReaderAsync();
        var tables = new List<string>();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        return [.. tables];
    }

    private static async Task<int> CountAppliedVersions(NpgsqlDataSource dataSource)
    {
        await using var command = dataSource.CreateCommand("SELECT COUNT(*) FROM schema_versions;");
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static async Task InsertUser(
        NpgsqlDataSource dataSource,
        Guid id,
        string email,
        string normalizedEmail)
    {
        await using var command = dataSource.CreateCommand(
            """
            INSERT INTO users(id, email, normalized_email, password_hash)
            VALUES (@id, @email, @normalized_email, 'hash');
            """);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("email", email);
        command.Parameters.AddWithValue("normalized_email", normalizedEmail);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task InsertTask(
        NpgsqlDataSource dataSource,
        Guid ownerId,
        string title,
        string? description,
        string status)
    {
        await using var command = dataSource.CreateCommand(
            """
            INSERT INTO tasks(id, owner_id, title, description, status, due_date)
            VALUES (@id, @owner_id, @title, @description, @status, NULL);
            """);
        command.Parameters.AddWithValue("id", Guid.NewGuid());
        command.Parameters.AddWithValue("owner_id", ownerId);
        command.Parameters.AddWithValue("title", title);
        command.Parameters.AddWithValue("description", (object?)description ?? DBNull.Value);
        command.Parameters.AddWithValue("status", status);
        await command.ExecuteNonQueryAsync();
    }
}
