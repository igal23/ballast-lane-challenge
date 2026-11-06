using TaskBoard.Application.Abstractions;
using TaskBoard.Application.Common;

namespace TaskBoard.Application.Tasks;

public sealed class ListTasks(ITaskRepository repository)
{
    public async Task<Result<IReadOnlyList<TaskDto>>> ExecuteAsync(
        Guid ownerId,
        CancellationToken cancellationToken)
    {
        var tasks = await repository.ListAsync(ownerId, cancellationToken);
        return Result<IReadOnlyList<TaskDto>>.Success(tasks.Select(TaskDto.From).ToArray());
    }
}
