using TaskBoard.Application.Abstractions;
using TaskBoard.Application.Common;
using TaskBoard.Application.Tasks;
using TaskBoard.Domain.Tasks;
using DomainTaskStatus = TaskBoard.Domain.Tasks.TaskStatus;

namespace TaskBoard.UnitTests.Application;

public sealed class ReplaceTaskTests
{
    [Fact]
    public async Task Execute_ExistingTask_UsesBothIdsAppliesDomainRulesThenPersists()
    {
        var ownerId = Guid.NewGuid();
        var task = TaskItem.Rehydrate(Guid.NewGuid(), ownerId, "Old", null, DomainTaskStatus.Pending, null);
        var repository = new StubTaskRepository(task);

        var result = await new ReplaceTask(repository, new FixedDate()).ExecuteAsync(
            task.Id, ownerId, new TaskWriteCommand(" New ", " Description ", "Completed", new DateOnly(2026, 11, 6)), default);

        Assert.Equal(Outcome.Success, result.Outcome);
        Assert.Equal((task.Id, ownerId), (repository.RequestedTaskId, repository.RequestedOwnerId));
        Assert.Same(task, repository.Replaced);
        Assert.Equal("New", task.Title);
        Assert.Equal(DomainTaskStatus.Completed, task.Status);
    }

    [Fact]
    public async Task Execute_MissingTask_ReturnsNotFoundWithoutPersistence()
    {
        var repository = new StubTaskRepository(null);

        var result = await new ReplaceTask(repository, new FixedDate()).ExecuteAsync(
            Guid.NewGuid(), Guid.NewGuid(), new TaskWriteCommand("Task", null, "Pending", null), default);

        Assert.Equal(Outcome.NotFound, result.Outcome);
        Assert.Null(repository.Replaced);
    }

    private sealed class FixedDate : ICurrentDate { public DateOnly UtcToday => new(2026, 11, 6); }
    private sealed class StubTaskRepository(TaskItem? task) : ITaskRepository
    {
        public Guid RequestedTaskId { get; private set; }
        public Guid RequestedOwnerId { get; private set; }
        public TaskItem? Replaced { get; private set; }
        public Task<TaskItem?> GetAsync(Guid id, Guid ownerId, CancellationToken cancellationToken)
        {
            RequestedTaskId = id;
            RequestedOwnerId = ownerId;
            return Task.FromResult(task);
        }
        public Task<bool> ReplaceAsync(TaskItem replacement, CancellationToken cancellationToken)
        {
            Replaced = replacement;
            return Task.FromResult(true);
        }
        public Task AddAsync(TaskItem item, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<TaskItem>> ListAsync(Guid ownerId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> DeleteAsync(Guid id, Guid ownerId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
