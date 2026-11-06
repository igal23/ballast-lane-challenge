using TaskBoard.Domain.Tasks;

namespace TaskBoard.Application.Abstractions;

public interface ITaskRepository
{
    Task AddAsync(TaskItem task, CancellationToken cancellationToken);

    Task<IReadOnlyList<TaskItem>> ListAsync(Guid ownerId, CancellationToken cancellationToken);

    Task<TaskItem?> GetAsync(Guid id, Guid ownerId, CancellationToken cancellationToken);

    Task<bool> ReplaceAsync(TaskItem task, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid id, Guid ownerId, CancellationToken cancellationToken);
}
