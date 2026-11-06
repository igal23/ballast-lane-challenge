using TaskBoard.Application.Abstractions;
using TaskBoard.Application.Common;

namespace TaskBoard.Application.Tasks;

public sealed class GetTask(ITaskRepository repository)
{
    public async Task<Result<TaskDto>> ExecuteAsync(
        Guid id,
        Guid ownerId,
        CancellationToken cancellationToken)
    {
        var task = await repository.GetAsync(id, ownerId, cancellationToken);
        return task is null
            ? Result<TaskDto>.Failure(Outcome.NotFound, new AppError("task.not_found"))
            : Result<TaskDto>.Success(TaskDto.From(task));
    }
}
