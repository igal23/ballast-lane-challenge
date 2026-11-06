using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TaskBoard.Api.Contracts;
using TaskBoard.Api.Controllers;
using TaskBoard.Application.Abstractions;
using TaskBoard.Application.Tasks;
using TaskBoard.Domain.Tasks;

namespace TaskBoard.UnitTests.Api;

public sealed class TasksControllerTests
{
    [Fact]
    public async Task CreateAndList_UseSubjectOwnerAndMap201Then200()
    {
        var owner = Guid.NewGuid();
        var repository = new RecordingRepository();
        var controller = new TasksController(
            new CreateTask(repository, new FixedDate()),
            new ListTasks(repository));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(JwtRegisteredClaimNames.Sub, owner.ToString())], "Bearer"))
            }
        };

        var createdAction = await controller.Create(
            new TaskWriteRequest("Task", null, "Pending", null), default);
        var listedAction = await controller.List(default);

        var created = Assert.IsType<CreatedResult>(createdAction.Result);
        Assert.Equal(201, created.StatusCode);
        Assert.Equal($"/api/tasks/{repository.Added!.Id}", created.Location);
        var ok = Assert.IsType<OkObjectResult>(listedAction.Result);
        Assert.Empty(Assert.IsAssignableFrom<IReadOnlyList<TaskResponse>>(ok.Value));
        Assert.Equal(owner, repository.Added.OwnerId);
        Assert.Equal(owner, repository.ListOwner);
    }

    private sealed class FixedDate : ICurrentDate
    {
        public DateOnly UtcToday => new(2026, 11, 6);
    }

    private sealed class RecordingRepository : ITaskRepository
    {
        public TaskItem? Added { get; private set; }
        public Guid ListOwner { get; private set; }
        public Task AddAsync(TaskItem task, CancellationToken cancellationToken)
        {
            Added = task;
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<TaskItem>> ListAsync(Guid ownerId, CancellationToken cancellationToken)
        {
            ListOwner = ownerId;
            return Task.FromResult<IReadOnlyList<TaskItem>>([]);
        }
    }
}
