using Npgsql;
using TaskBoard.Domain.Tasks;
using TaskBoard.Infrastructure.Persistence;
using TaskBoard.IntegrationTests.Support;
using DomainTaskStatus = TaskBoard.Domain.Tasks.TaskStatus;

namespace TaskBoard.IntegrationTests.Persistence;

public sealed class NpgsqlTaskRepositoryTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _postgres;
    public NpgsqlTaskRepositoryTests(PostgresFixture postgres) => _postgres = postgres;

    [Fact]
    public async Task AddAndList_RoundTripsFieldsAndSqlLookingText()
    {
        await using var dataSource = NpgsqlDataSource.Create(_postgres.ConnectionString);
        await new DatabaseMigrator(dataSource).MigrateAsync(default);
        var owner = await AddOwner(dataSource);
        var repository = new NpgsqlTaskRepository(dataSource);
        var text = "Review Bob's '; DROP TABLE tasks; --";
        var task = TaskItem.Rehydrate(Guid.NewGuid(), owner, text, text, DomainTaskStatus.Wip, new DateOnly(2026, 12, 1));

        await repository.AddAsync(task, default);
        var listed = await repository.ListAsync(owner, default);

        Assert.Contains(listed, item =>
            item.Id == task.Id && item.Title == text && item.Description == text
            && item.Status == DomainTaskStatus.Wip && item.DueDate == task.DueDate);
        await using var command = dataSource.CreateCommand("SELECT to_regclass('public.tasks') IS NOT NULL;");
        Assert.True((bool)(await command.ExecuteScalarAsync())!);
    }

    [Fact]
    public async Task List_ReturnsOnlyOwnerInDeterministicOrderAndEmptyForUnknownOwner()
    {
        await using var dataSource = NpgsqlDataSource.Create(_postgres.ConnectionString);
        await new DatabaseMigrator(dataSource).MigrateAsync(default);
        var owner = await AddOwner(dataSource);
        var other = await AddOwner(dataSource);
        var repository = new NpgsqlTaskRepository(dataSource);
        var late = NewTask(owner, DomainTaskStatus.Pending, new DateOnly(2026, 12, 2));
        var early = NewTask(owner, DomainTaskStatus.Wip, new DateOnly(2026, 12, 1));
        var noDate = NewTask(owner, DomainTaskStatus.Pending, null);
        var completed = NewTask(owner, DomainTaskStatus.Completed, new DateOnly(2026, 11, 30));
        await repository.AddAsync(completed, default);
        await repository.AddAsync(noDate, default);
        await repository.AddAsync(late, default);
        await repository.AddAsync(early, default);
        await repository.AddAsync(NewTask(other, DomainTaskStatus.Pending, null), default);

        var listed = await repository.ListAsync(owner, default);

        Assert.Equal([early.Id, late.Id, noDate.Id, completed.Id], listed.Select(item => item.Id));
        Assert.Empty(await repository.ListAsync(Guid.NewGuid(), default));
    }

    [Fact]
    public async Task GetReplaceDelete_RequireMatchingOwnerAndPersistLastWriteWins()
    {
        await using var dataSource = NpgsqlDataSource.Create(_postgres.ConnectionString);
        await new DatabaseMigrator(dataSource).MigrateAsync(default);
        var owner = await AddOwner(dataSource);
        var otherOwner = await AddOwner(dataSource);
        var repository = new NpgsqlTaskRepository(dataSource);
        var task = NewTask(owner, DomainTaskStatus.Pending, null);
        await repository.AddAsync(task, default);

        Assert.Null(await repository.GetAsync(task.Id, otherOwner, default));
        var foreignReplacement = TaskItem.Rehydrate(
            task.Id, otherOwner, "Foreign", null, DomainTaskStatus.Completed, null);
        Assert.False(await repository.ReplaceAsync(foreignReplacement, default));
        Assert.Equal(task.Title, (await repository.GetAsync(task.Id, owner, default))!.Title);

        task.Replace("First write", null, DomainTaskStatus.Wip, null, new DateOnly(2026, 11, 6));
        Assert.True(await repository.ReplaceAsync(task, default));
        task.Replace("Last write", "Saved", DomainTaskStatus.Completed, null, new DateOnly(2026, 11, 6));
        Assert.True(await repository.ReplaceAsync(task, default));
        var saved = await repository.GetAsync(task.Id, owner, default);
        Assert.Equal("Last write", saved!.Title);
        Assert.Equal("Saved", saved.Description);
        Assert.Equal(DomainTaskStatus.Completed, saved.Status);

        Assert.False(await repository.DeleteAsync(task.Id, otherOwner, default));
        Assert.NotNull(await repository.GetAsync(task.Id, owner, default));
        Assert.True(await repository.DeleteAsync(task.Id, owner, default));
        Assert.Null(await repository.GetAsync(task.Id, owner, default));
    }

    private static TaskItem NewTask(Guid owner, DomainTaskStatus status, DateOnly? dueDate) =>
        TaskItem.Rehydrate(Guid.NewGuid(), owner, Guid.NewGuid().ToString(), null, status, dueDate);

    private static async Task<Guid> AddOwner(NpgsqlDataSource dataSource)
    {
        var id = Guid.NewGuid();
        await using var command = dataSource.CreateCommand(
            "INSERT INTO users(id,email,normalized_email,password_hash) VALUES(@id,@email,@email,'hash');");
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("email", $"{id:N}@example.com");
        await command.ExecuteNonQueryAsync();
        return id;
    }
}
