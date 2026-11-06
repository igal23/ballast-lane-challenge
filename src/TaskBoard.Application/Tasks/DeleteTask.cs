using TaskBoard.Application.Abstractions;
using TaskBoard.Application.Common;

namespace TaskBoard.Application.Tasks;

public sealed class DeleteTask(ITaskRepository repository)
{
    public async Task<Result<bool>> ExecuteAsync(
        Guid id,
        Guid ownerId,
        CancellationToken cancellationToken)
    {
        return await repository.DeleteAsync(id, ownerId, cancellationToken)
            ? Result<bool>.Success(true)
            : Result<bool>.Failure(Outcome.NotFound, new AppError("task.not_found"));
    }
}
