using TaskBoard.Application.Abstractions;
using TaskBoard.Application.Common;
using TaskBoard.Domain.Tasks;
using TaskBoard.Domain.Validation;
using DomainTaskStatus = TaskBoard.Domain.Tasks.TaskStatus;

namespace TaskBoard.Application.Tasks;

public sealed class CreateTask(ITaskRepository repository, ICurrentDate currentDate)
{
    public async Task<Result<TaskDto>> ExecuteAsync(
        Guid ownerId,
        TaskWriteCommand command,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<DomainTaskStatus>(command.Status, ignoreCase: false, out var status)
            || !Enum.IsDefined(status))
        {
            return Validation("task.status.invalid", "status");
        }

        try
        {
            var task = TaskItem.Create(
                Guid.NewGuid(),
                ownerId,
                command.Title,
                command.Description,
                status,
                command.DueDate,
                currentDate.UtcToday);
            await repository.AddAsync(task, cancellationToken);
            return Result<TaskDto>.Success(TaskDto.From(task));
        }
        catch (DomainValidationException exception)
        {
            return Validation(exception.Code, exception.Field);
        }
    }

    private static Result<TaskDto> Validation(string code, string field) =>
        Result<TaskDto>.Failure(Outcome.ValidationError, new AppError(code, field));
}
