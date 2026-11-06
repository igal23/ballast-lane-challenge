using TaskBoard.Application.Abstractions;
using TaskBoard.Application.Common;
using TaskBoard.Application.Tasks;
using TaskBoard.Domain.Tasks;
using DomainTaskStatus = TaskBoard.Domain.Tasks.TaskStatus;

namespace TaskBoard.UnitTests.Application;

public sealed class CreateTaskTests
{
    private static readonly DateOnly Today = new(2026, 11, 6);

    [Fact]
    public async Task Execute_ValidCommand_AssignsAuthenticatedOwnerAndPersists()
    {
        var ownerId = Guid.NewGuid();
        var repository = new RecordingTaskRepository();
        var useCase = new CreateTask(repository, new FixedDate(Today));

        var result = await useCase.ExecuteAsync(
            ownerId,
            new TaskWriteCommand(" Task ", " Description ", "Wip", Today),
            default);

        Assert.Equal(Outcome.Success, result.Outcome);
        Assert.Equal(ownerId, repository.Added!.OwnerId);
        Assert.Equal("Task", result.Value!.Title);
        Assert.Equal("Wip", result.Value.Status);
        Assert.Equal(Today, result.Value.DueDate);
    }

    public static TheoryData<string, string, DateOnly?, string> InvalidCommands => new()
    {
        { " ", "Pending", Today, "task.title.required" },
        { "Task", "pending", Today, "task.status.invalid" },
        { "Task", "Unknown", Today, "task.status.invalid" },
        { "Task", "Pending", Today.AddDays(-1), "task.due_date.in_past" }
    };

    [Theory]
    [MemberData(nameof(InvalidCommands))]
    public async Task Execute_InvalidCommand_ReturnsStableValidationWithoutPersistence(
        string title,
        string status,
        DateOnly? dueDate,
        string expectedCode)
    {
        var repository = new RecordingTaskRepository();
        var useCase = new CreateTask(repository, new FixedDate(Today));

        var result = await useCase.ExecuteAsync(
            Guid.NewGuid(),
            new TaskWriteCommand(title, null, status, dueDate),
            default);

        Assert.Equal(Outcome.ValidationError, result.Outcome);
        Assert.Equal(expectedCode, Assert.Single(result.Errors).Code);
        Assert.Null(repository.Added);
    }

    private sealed class FixedDate(DateOnly today) : ICurrentDate
    {
        public DateOnly UtcToday => today;
    }

    private sealed class RecordingTaskRepository : ITaskRepository
    {
        public TaskItem? Added { get; private set; }
        public Task AddAsync(TaskItem task, CancellationToken cancellationToken)
        {
            Added = task;
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<TaskItem>> ListAsync(Guid ownerId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<TaskItem>>([]);
    }
}
