using Npgsql;
using NpgsqlTypes;
using TaskBoard.Application.Abstractions;

namespace TaskBoard.Infrastructure.Persistence;

public sealed class DevelopmentSeeder(
    NpgsqlDataSource dataSource,
    IPasswordHasher passwordHasher)
{
    public const string DemoEmail = "demo@taskboard.local";
    public const string DemoPassword = "TaskBoard demo passphrase 2026!";
    private static readonly Guid DemoUserId = Guid.Parse("10000000-0000-0000-0000-000000000001");

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using (var insertUser = new NpgsqlCommand(
            """
            INSERT INTO users(id, email, normalized_email, password_hash)
            VALUES (@id, @email, @email, @password_hash)
            ON CONFLICT (normalized_email) DO NOTHING;
            """,
            connection,
            transaction))
        {
            insertUser.Parameters.Add("id", NpgsqlDbType.Uuid).Value = DemoUserId;
            insertUser.Parameters.Add("email", NpgsqlDbType.Varchar).Value = DemoEmail;
            insertUser.Parameters.Add("password_hash", NpgsqlDbType.Text).Value =
                passwordHasher.Hash(DemoPassword);
            await insertUser.ExecuteNonQueryAsync(cancellationToken);
        }

        Guid ownerId;
        await using (var selectUser = new NpgsqlCommand(
            "SELECT id FROM users WHERE normalized_email = @email;",
            connection,
            transaction))
        {
            selectUser.Parameters.Add("email", NpgsqlDbType.Varchar).Value = DemoEmail;
            ownerId = (Guid)(await selectUser.ExecuteScalarAsync(cancellationToken))!;
        }

        var seeds = new[]
        {
            (Guid.Parse("20000000-0000-0000-0000-000000000001"), "Plan the demo", "Pending"),
            (Guid.Parse("20000000-0000-0000-0000-000000000002"), "Build TaskBoard", "Wip"),
            (Guid.Parse("20000000-0000-0000-0000-000000000003"), "Approve the design", "Completed")
        };
        foreach (var (id, title, status) in seeds)
        {
            await using var insertTask = new NpgsqlCommand(
                """
                INSERT INTO tasks(id, owner_id, title, description, status, due_date)
                VALUES (@id, @owner_id, @title, NULL, @status, NULL)
                ON CONFLICT (id) DO NOTHING;
                """,
                connection,
                transaction);
            insertTask.Parameters.Add("id", NpgsqlDbType.Uuid).Value = id;
            insertTask.Parameters.Add("owner_id", NpgsqlDbType.Uuid).Value = ownerId;
            insertTask.Parameters.Add("title", NpgsqlDbType.Varchar).Value = title;
            insertTask.Parameters.Add("status", NpgsqlDbType.Varchar).Value = status;
            await insertTask.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }
}
