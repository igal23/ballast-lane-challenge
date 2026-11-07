using TaskBoard.Domain.Validation;

namespace TaskBoard.Domain.Tasks;

public sealed class TaskItem
{
    private TaskItem(
        Guid id,
        Guid ownerId,
        string title,
        string? description,
        TaskStatus status,
        DateOnly? dueDate)
    {
        Id = id;
        OwnerId = ownerId;
        Title = title;
        Description = description;
        Status = status;
        DueDate = dueDate;
    }

    public Guid Id { get; }

    public Guid OwnerId { get; }

    public string Title { get; private set; }

    public string? Description { get; private set; }

    public TaskStatus Status { get; private set; }

    public DateOnly? DueDate { get; private set; }

    public static TaskItem Create(
        Guid id,
        Guid ownerId,
        string title,
        string? description,
        TaskStatus status,
        DateOnly? dueDate,
        DateOnly utcToday)
    {
        var normalizedTitle = NormalizeTitle(title);
        var normalizedDescription = NormalizeDescription(description);
        ValidateStatus(status);
        ValidateDueDate(dueDate, utcToday);

        return new TaskItem(
            id,
            ownerId,
            normalizedTitle,
            normalizedDescription,
            status,
            dueDate);
    }

    public static TaskItem Rehydrate(
        Guid id,
        Guid ownerId,
        string title,
        string? description,
        TaskStatus status,
        DateOnly? dueDate)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("task.id.required", "id");
        }

        if (ownerId == Guid.Empty)
        {
            throw new DomainValidationException("task.owner_id.required", "ownerId");
        }

        ValidatePersistedTitle(title);
        ValidatePersistedDescription(description);
        ValidateStatus(status);

        return new TaskItem(id, ownerId, title, description, status, dueDate);
    }

    public void Replace(
        string title,
        string? description,
        TaskStatus status,
        DateOnly? dueDate,
        DateOnly utcToday)
    {
        var normalizedTitle = NormalizeTitle(title);
        var normalizedDescription = NormalizeDescription(description);
        ValidateStatus(status);
        if (dueDate != DueDate)
        {
            ValidateDueDate(dueDate, utcToday);
        }

        Title = normalizedTitle;
        Description = normalizedDescription;
        Status = status;
        DueDate = dueDate;
    }

    private static string NormalizeTitle(string title)
    {
        var normalized = title?.Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            throw new DomainValidationException("task.title.required", "title");
        }

        if (normalized.Length > 200)
        {
            throw new DomainValidationException("task.title.too_long", "title");
        }

        return normalized;
    }

    private static string? NormalizeDescription(string? description)
    {
        var normalized = description?.Trim();
        if (normalized is { Length: > 2000 })
        {
            throw new DomainValidationException("task.description.too_long", "description");
        }

        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }

    private static void ValidatePersistedTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainValidationException("task.title.required", "title");
        }

        if (title.Length > 200)
        {
            throw new DomainValidationException("task.title.too_long", "title");
        }
    }

    private static void ValidatePersistedDescription(string? description)
    {
        if (description is { Length: > 2000 })
        {
            throw new DomainValidationException("task.description.too_long", "description");
        }
    }

    private static void ValidateDueDate(DateOnly? dueDate, DateOnly utcToday)
    {
        if (dueDate < utcToday)
        {
            throw new DomainValidationException("task.due_date.in_past", "dueDate");
        }
    }

    private static void ValidateStatus(TaskStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw new DomainValidationException("task.status.invalid", "status");
        }
    }
}
