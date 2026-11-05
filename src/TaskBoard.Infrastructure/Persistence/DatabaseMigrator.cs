using System.Reflection;
using Npgsql;
using NpgsqlTypes;

namespace TaskBoard.Infrastructure.Persistence;

public sealed class DatabaseMigrator
{
    private const string InitialMigrationResource =
        "TaskBoard.Infrastructure.Persistence.Migrations.001_Initial.sql";

    private readonly NpgsqlDataSource _dataSource;

    public DatabaseMigrator(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);

        await using (var createLedger = new NpgsqlCommand(
            """
            CREATE TABLE IF NOT EXISTS schema_versions
            (
                version integer PRIMARY KEY,
                applied_at timestamptz NOT NULL DEFAULT now()
            );
            """,
            connection))
        {
            await createLedger.ExecuteNonQueryAsync(cancellationToken);
        }

        if (await IsAppliedAsync(connection, 1, cancellationToken))
        {
            return;
        }

        var sql = await ReadMigrationAsync(InitialMigrationResource, cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using (var applyMigration = new NpgsqlCommand(sql, connection, transaction))
        {
            await applyMigration.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var recordVersion = new NpgsqlCommand(
            "INSERT INTO schema_versions(version) VALUES (@version);",
            connection,
            transaction))
        {
            recordVersion.Parameters.AddWithValue("version", NpgsqlDbType.Integer, 1);
            await recordVersion.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task<bool> IsAppliedAsync(
        NpgsqlConnection connection,
        int version,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            "SELECT EXISTS(SELECT 1 FROM schema_versions WHERE version = @version);",
            connection);
        command.Parameters.AddWithValue("version", NpgsqlDbType.Integer, version);
        return (bool)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    private static async Task<string> ReadMigrationAsync(
        string resourceName,
        CancellationToken cancellationToken)
    {
        await using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded migration not found: {resourceName}");
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}
