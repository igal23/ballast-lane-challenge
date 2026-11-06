using TaskBoard.Domain.Tasks;
using TaskBoard.Domain.Validation;
using DomainTaskStatus = TaskBoard.Domain.Tasks.TaskStatus;

namespace TaskBoard.UnitTests.Domain;

public sealed class TaskItemTests
{
    private static readonly DateOnly Today = new(2026, 11, 5);

    [Fact]
    public void Create_BlankTitle_ThrowsRequiredError()
    {
        var exception = Assert.Throws<DomainValidationException>(() =>
            Create(title: "   "));

        Assert.Equal("task.title.required", exception.Code);
        Assert.Equal("title", exception.Field);
    }

    [Fact]
    public void Create_NullTitle_ThrowsRequiredError()
    {
        var exception = Assert.Throws<DomainValidationException>(() =>
            Create(title: null!));

        Assert.Equal("task.title.required", exception.Code);
        Assert.Equal("title", exception.Field);
    }

    [Fact]
    public void Create_TitleLongerThan200Characters_ThrowsTooLongError()
    {
        var exception = Assert.Throws<DomainValidationException>(() =>
            Create(title: new string('x', 201)));

        Assert.Equal("task.title.too_long", exception.Code);
        Assert.Equal("title", exception.Field);
    }

    [Fact]
    public void Create_DescriptionLongerThan2000Characters_ThrowsTooLongError()
    {
        var exception = Assert.Throws<DomainValidationException>(() =>
            Create(description: new string('x', 2001)));

        Assert.Equal("task.description.too_long", exception.Code);
        Assert.Equal("description", exception.Field);
    }

    [Fact]
    public void Create_DueDateBeforeUtcToday_ThrowsPastDateError()
    {
        var exception = Assert.Throws<DomainValidationException>(() =>
            Create(dueDate: Today.AddDays(-1)));

        Assert.Equal("task.due_date.in_past", exception.Code);
        Assert.Equal("dueDate", exception.Field);
    }

    [Fact]
    public void Create_UndefinedStatus_ThrowsInvalidStatusError()
    {
        var exception = Assert.Throws<DomainValidationException>(() =>
            Create(status: (DomainTaskStatus)999));

        Assert.Equal("task.status.invalid", exception.Code);
        Assert.Equal("status", exception.Field);
    }

    [Fact]
    public void Create_ValidInput_NormalizesTextAndKeepsTodayDueDate()
    {
        var task = Create(
            title: "  Ship TaskBoard  ",
            description: "  Verify the demo  ",
            dueDate: Today);

        Assert.Equal("Ship TaskBoard", task.Title);
        Assert.Equal("Verify the demo", task.Description);
        Assert.Equal(Today, task.DueDate);
        Assert.Equal(DomainTaskStatus.Pending, task.Status);
    }

    [Fact]
    public void Create_BlankDescriptionAndNoDueDate_StoresNulls()
    {
        var task = Create(description: "   ", dueDate: null);

        Assert.Null(task.Description);
        Assert.Null(task.DueDate);
    }

    [Fact]
    public void Rehydrate_OverdueTask_PreservesPersistedValues()
    {
        var overdueDate = Today.AddDays(-10);

        var task = TaskItem.Rehydrate(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "  Persisted title  ",
            "  Persisted description  ",
            DomainTaskStatus.Wip,
            overdueDate);

        Assert.Equal("  Persisted title  ", task.Title);
        Assert.Equal("  Persisted description  ", task.Description);
        Assert.Equal(DomainTaskStatus.Wip, task.Status);
        Assert.Equal(overdueDate, task.DueDate);
    }

    [Fact]
    public void Rehydrate_EmptyId_ThrowsRequiredError()
    {
        var exception = Assert.Throws<DomainValidationException>(() =>
            Rehydrate(id: Guid.Empty));

        Assert.Equal("task.id.required", exception.Code);
        Assert.Equal("id", exception.Field);
    }

    [Fact]
    public void Rehydrate_EmptyOwnerId_ThrowsRequiredError()
    {
        var exception = Assert.Throws<DomainValidationException>(() =>
            Rehydrate(ownerId: Guid.Empty));

        Assert.Equal("task.owner_id.required", exception.Code);
        Assert.Equal("ownerId", exception.Field);
    }

    [Fact]
    public void Rehydrate_BlankTitle_ThrowsRequiredError()
    {
        var exception = Assert.Throws<DomainValidationException>(() =>
            Rehydrate(title: "   "));

        Assert.Equal("task.title.required", exception.Code);
        Assert.Equal("title", exception.Field);
    }

    [Fact]
    public void Rehydrate_TitleLongerThan200Characters_ThrowsTooLongError()
    {
        var exception = Assert.Throws<DomainValidationException>(() =>
            Rehydrate(title: new string('x', 201)));

        Assert.Equal("task.title.too_long", exception.Code);
        Assert.Equal("title", exception.Field);
    }

    [Fact]
    public void Rehydrate_DescriptionLongerThan2000Characters_ThrowsTooLongError()
    {
        var exception = Assert.Throws<DomainValidationException>(() =>
            Rehydrate(description: new string('x', 2001)));

        Assert.Equal("task.description.too_long", exception.Code);
        Assert.Equal("description", exception.Field);
    }

    [Fact]
    public void Rehydrate_UndefinedStatus_ThrowsInvalidStatusError()
    {
        var exception = Assert.Throws<DomainValidationException>(() =>
            Rehydrate(status: (DomainTaskStatus)999));

        Assert.Equal("task.status.invalid", exception.Code);
        Assert.Equal("status", exception.Field);
    }

    [Fact]
    public void Replace_StatusTransitions_AreUnrestricted()
    {
        var task = Create(status: DomainTaskStatus.Pending);

        task.Replace("Task", null, DomainTaskStatus.Completed, null, Today);
        Assert.Equal(DomainTaskStatus.Completed, task.Status);

        task.Replace("Task", null, DomainTaskStatus.Wip, null, Today);
        Assert.Equal(DomainTaskStatus.Wip, task.Status);
    }

    [Fact]
    public void Replace_UnchangedOverdueDate_AllowsOtherChanges()
    {
        var overdueDate = Today.AddDays(-1);
        var task = TaskItem.Rehydrate(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Old title",
            null,
            DomainTaskStatus.Pending,
            overdueDate);

        task.Replace(
            "  New title  ",
            "  New description  ",
            DomainTaskStatus.Completed,
            overdueDate,
            Today);

        Assert.Equal("New title", task.Title);
        Assert.Equal("New description", task.Description);
        Assert.Equal(DomainTaskStatus.Completed, task.Status);
        Assert.Equal(overdueDate, task.DueDate);
    }

    [Fact]
    public void Replace_ChangedPastDate_ThrowsWithoutChangingAnyValues()
    {
        var originalDueDate = Today.AddDays(1);
        var task = Create(
            title: "Original title",
            description: "Original description",
            status: DomainTaskStatus.Pending,
            dueDate: originalDueDate);

        var exception = Assert.Throws<DomainValidationException>(() =>
            task.Replace(
                "Changed title",
                "Changed description",
                DomainTaskStatus.Completed,
                Today.AddDays(-1),
                Today));

        Assert.Equal("task.due_date.in_past", exception.Code);
        Assert.Equal("Original title", task.Title);
        Assert.Equal("Original description", task.Description);
        Assert.Equal(DomainTaskStatus.Pending, task.Status);
        Assert.Equal(originalDueDate, task.DueDate);
    }

    [Fact]
    public void Replace_UndefinedStatus_ThrowsWithoutChangingAnyValues()
    {
        var task = Create(
            title: "Original title",
            description: "Original description",
            status: DomainTaskStatus.Pending,
            dueDate: Today.AddDays(1));

        var exception = Assert.Throws<DomainValidationException>(() =>
            task.Replace(
                "Changed title",
                "Changed description",
                (DomainTaskStatus)999,
                Today.AddDays(2),
                Today));

        Assert.Equal("task.status.invalid", exception.Code);
        Assert.Equal("Original title", task.Title);
        Assert.Equal("Original description", task.Description);
        Assert.Equal(DomainTaskStatus.Pending, task.Status);
        Assert.Equal(Today.AddDays(1), task.DueDate);
    }

    private static TaskItem Create(
        string title = "Task",
        string? description = null,
        DomainTaskStatus status = DomainTaskStatus.Pending,
        DateOnly? dueDate = null)
    {
        return TaskItem.Create(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            title,
            description,
            status,
            dueDate,
            Today);
    }

    private static TaskItem Rehydrate(
        Guid? id = null,
        Guid? ownerId = null,
        string title = "Task",
        string? description = null,
        DomainTaskStatus status = DomainTaskStatus.Pending,
        DateOnly? dueDate = null) =>
        TaskItem.Rehydrate(
            id ?? Guid.Parse("11111111-1111-1111-1111-111111111111"),
            ownerId ?? Guid.Parse("22222222-2222-2222-2222-222222222222"),
            title,
            description,
            status,
            dueDate);
}
