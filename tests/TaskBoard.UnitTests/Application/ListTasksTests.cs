using TaskBoard.Application.Abstractions;
using TaskBoard.Application.Common;
using TaskBoard.Application.Tasks;
using TaskBoard.Domain.Tasks;
using DomainTaskStatus = TaskBoard.Domain.Tasks.TaskStatus;

namespace TaskBoard.UnitTests.Application;

public sealed class ListTasksTests
{
    [Fact]
    public async Task Execute_PassesOwnerAndPreservesRepositoryOrder()
    {
        var ownerId = Guid.NewGuid();
        var first = TaskItem.Rehydrate(Guid.NewGuid(), ownerId, "First", null, DomainTaskStatus.Wip, null);
        var second = TaskItem.Rehydrate(Guid.NewGuid(), ownerId, "Second", null, DomainTaskStatus.Completed, null);
        var repository = new StubRepository([first, second]);

        var result = await new ListTasks(repository).ExecuteAsync(ownerId, default);

        Assert.Equal(Outcome.Success, result.Outcome);
        Assert.Equal(ownerId, repository.RequestedOwnerId);
        Assert.Equal([first.Id, second.Id], result.Value!.Select(task => task.Id));
    }

    private sealed class StubRepository(IReadOnlyList<TaskItem> tasks) : ITaskRepository
    {
        public Guid RequestedOwnerId { get; private set; }
        public Task AddAsync(TaskItem task, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<TaskItem>> ListAsync(Guid ownerId, CancellationToken cancellationToken)
        {
            RequestedOwnerId = ownerId;
            return Task.FromResult(tasks);
        }
    }
}
