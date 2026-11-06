using TaskBoard.Application.Abstractions;
using TaskBoard.Application.Common;
using TaskBoard.Application.Tasks;
using TaskBoard.Domain.Tasks;

namespace TaskBoard.UnitTests.Application;

public sealed class DeleteTaskTests
{
    [Theory]
    [InlineData(true, Outcome.Success)]
    [InlineData(false, Outcome.NotFound)]
    public async Task Execute_UsesBothIdsAndMapsAffectedRow(bool deleted, Outcome expected)
    {
        var repository = new StubTaskRepository(deleted);
        var taskId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        var result = await new DeleteTask(repository).ExecuteAsync(taskId, ownerId, default);

        Assert.Equal(expected, result.Outcome);
        Assert.Equal((taskId, ownerId), (repository.TaskId, repository.OwnerId));
    }

    private sealed class StubTaskRepository(bool deleted) : ITaskRepository
    {
        public Guid TaskId { get; private set; }
        public Guid OwnerId { get; private set; }
        public Task<bool> DeleteAsync(Guid id, Guid ownerId, CancellationToken cancellationToken)
        {
            TaskId = id;
            OwnerId = ownerId;
            return Task.FromResult(deleted);
        }
        public Task AddAsync(TaskItem task, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<TaskItem>> ListAsync(Guid ownerId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<TaskItem?> GetAsync(Guid id, Guid ownerId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> ReplaceAsync(TaskItem task, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
