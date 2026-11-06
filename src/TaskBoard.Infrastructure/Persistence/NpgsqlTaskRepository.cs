using Npgsql;
using NpgsqlTypes;
using TaskBoard.Application.Abstractions;
using TaskBoard.Domain.Tasks;
using DomainTaskStatus = TaskBoard.Domain.Tasks.TaskStatus;

namespace TaskBoard.Infrastructure.Persistence;

public sealed class NpgsqlTaskRepository(NpgsqlDataSource dataSource) : ITaskRepository
{
    public async Task AddAsync(TaskItem task, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            """
            INSERT INTO tasks(id, owner_id, title, description, status, due_date)
            VALUES (@id, @owner_id, @title, @description, @status, @due_date);
            """);
        command.Parameters.Add("id", NpgsqlDbType.Uuid).Value = task.Id;
        command.Parameters.Add("owner_id", NpgsqlDbType.Uuid).Value = task.OwnerId;
        command.Parameters.Add("title", NpgsqlDbType.Varchar).Value = task.Title;
        command.Parameters.Add("description", NpgsqlDbType.Varchar).Value = (object?)task.Description ?? DBNull.Value;
        command.Parameters.Add("status", NpgsqlDbType.Varchar).Value = task.Status.ToString();
        command.Parameters.Add("due_date", NpgsqlDbType.Date).Value = (object?)task.DueDate ?? DBNull.Value;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TaskItem>> ListAsync(
        Guid ownerId,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            """
            SELECT id, owner_id, title, description, status, due_date
            FROM tasks
            WHERE owner_id = @owner_id
            ORDER BY (status = 'Completed'), due_date NULLS LAST, id;
            """);
        command.Parameters.Add("owner_id", NpgsqlDbType.Uuid).Value = ownerId;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var tasks = new List<TaskItem>();
        var idOrdinal = reader.GetOrdinal("id");
        var ownerOrdinal = reader.GetOrdinal("owner_id");
        var titleOrdinal = reader.GetOrdinal("title");
        var descriptionOrdinal = reader.GetOrdinal("description");
        var statusOrdinal = reader.GetOrdinal("status");
        var dueDateOrdinal = reader.GetOrdinal("due_date");
        while (await reader.ReadAsync(cancellationToken))
        {
            tasks.Add(TaskItem.Rehydrate(
                reader.GetGuid(idOrdinal),
                reader.GetGuid(ownerOrdinal),
                reader.GetString(titleOrdinal),
                reader.IsDBNull(descriptionOrdinal) ? null : reader.GetString(descriptionOrdinal),
                Enum.Parse<DomainTaskStatus>(reader.GetString(statusOrdinal)),
                reader.IsDBNull(dueDateOrdinal) ? null : reader.GetFieldValue<DateOnly>(dueDateOrdinal)));
        }

        return tasks;
    }
}
