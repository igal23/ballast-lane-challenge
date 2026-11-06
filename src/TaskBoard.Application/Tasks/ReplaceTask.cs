using TaskBoard.Application.Abstractions;
using TaskBoard.Application.Common;
using TaskBoard.Domain.Validation;
using DomainTaskStatus = TaskBoard.Domain.Tasks.TaskStatus;

namespace TaskBoard.Application.Tasks;

public sealed class ReplaceTask(ITaskRepository repository, ICurrentDate currentDate)
{
    public async Task<Result<TaskDto>> ExecuteAsync(
        Guid id,
        Guid ownerId,
        TaskWriteCommand command,
        CancellationToken cancellationToken)
    {
        var task = await repository.GetAsync(id, ownerId, cancellationToken);
        if (task is null)
        {
            return NotFound();
        }

        if (!Enum.TryParse<DomainTaskStatus>(command.Status, false, out var status)
            || !Enum.IsDefined(status))
        {
            return Validation("task.status.invalid", "status");
        }

        try
        {
            task.Replace(
                command.Title,
                command.Description,
                status,
                command.DueDate,
                currentDate.UtcToday);
        }
        catch (DomainValidationException exception)
        {
            return Validation(exception.Code, exception.Field);
        }

        return await repository.ReplaceAsync(task, cancellationToken)
            ? Result<TaskDto>.Success(TaskDto.From(task))
            : NotFound();
    }

    private static Result<TaskDto> NotFound() =>
        Result<TaskDto>.Failure(Outcome.NotFound, new AppError("task.not_found"));

    private static Result<TaskDto> Validation(string code, string field) =>
        Result<TaskDto>.Failure(Outcome.ValidationError, new AppError(code, field));
}
