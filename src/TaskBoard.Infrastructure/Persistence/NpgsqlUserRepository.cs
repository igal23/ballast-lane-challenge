using Npgsql;
using TaskBoard.Application.Abstractions;
using TaskBoard.Domain.Users;

namespace TaskBoard.Infrastructure.Persistence;

public sealed class NpgsqlUserRepository(NpgsqlDataSource dataSource) : IUserRepository
{
    public async Task<UserAccount?> GetByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            """
            SELECT id, email, normalized_email, password_hash
            FROM users
            WHERE normalized_email = @normalized_email;
            """);
        command.Parameters.AddWithValue("normalized_email", normalizedEmail);
        return await ReadSingleAsync(command, cancellationToken);
    }

    public async Task<UserAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            """
            SELECT id, email, normalized_email, password_hash
            FROM users
            WHERE id = @id;
            """);
        command.Parameters.AddWithValue("id", id);
        return await ReadSingleAsync(command, cancellationToken);
    }

    public async Task<bool> AddAsync(UserAccount user, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            """
            INSERT INTO users(id, email, normalized_email, password_hash)
            VALUES (@id, @email, @normalized_email, @password_hash);
            """);
        command.Parameters.AddWithValue("id", user.Id);
        command.Parameters.AddWithValue("email", user.Email);
        command.Parameters.AddWithValue("normalized_email", user.NormalizedEmail);
        command.Parameters.AddWithValue("password_hash", user.PasswordHash);

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
            return true;
        }
        catch (PostgresException exception)
            when (exception.SqlState == PostgresErrorCodes.UniqueViolation
                  && exception.ConstraintName == "uq_users_normalized_email")
        {
            return false;
        }
    }

    private static async Task<UserAccount?> ReadSingleAsync(
        NpgsqlCommand command,
        CancellationToken cancellationToken)
    {
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new UserAccount(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3));
    }
}
