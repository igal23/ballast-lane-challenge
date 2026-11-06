using TaskBoard.Domain.Tasks;

namespace TaskBoard.Application.Tasks;

public sealed record TaskWriteCommand(
    string Title,
    string? Description,
    string Status,
    DateOnly? DueDate);

public sealed record TaskDto(
    Guid Id,
    string Title,
    string? Description,
    string Status,
    DateOnly? DueDate)
{
    public static TaskDto From(TaskItem task) =>
        new(task.Id, task.Title, task.Description, task.Status.ToString(), task.DueDate);
}
