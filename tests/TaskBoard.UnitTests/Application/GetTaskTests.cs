using TaskBoard.Application.Abstractions;
using TaskBoard.Application.Common;
using TaskBoard.Application.Tasks;
using TaskBoard.Domain.Tasks;

namespace TaskBoard.UnitTests.Application;

public sealed class GetTaskTests
{
    [Fact]
    public async Task Execute_ForwardsBothIdsAndMapsMissingToNotFound()
    {
        var repository = new StubTaskRepository();
        var taskId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        var result = await new GetTask(repository).ExecuteAsync(taskId, ownerId, default);

        Assert.Equal(Outcome.NotFound, result.Outcome);
        Assert.Equal(taskId, repository.RequestedTaskId);
        Assert.Equal(ownerId, repository.RequestedOwnerId);
        Assert.Equal("task.not_found", Assert.Single(result.Errors).Code);
    }

    private sealed class StubTaskRepository : ITaskRepository
    {
        public Guid RequestedTaskId { get; private set; }
        public Guid RequestedOwnerId { get; private set; }
        public Task<TaskItem?> GetAsync(Guid id, Guid ownerId, CancellationToken cancellationToken)
        {
            RequestedTaskId = id;
            RequestedOwnerId = ownerId;
            return Task.FromResult<TaskItem?>(null);
        }
        public Task AddAsync(TaskItem task, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<TaskItem>> ListAsync(Guid ownerId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> ReplaceAsync(TaskItem task, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> DeleteAsync(Guid id, Guid ownerId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
