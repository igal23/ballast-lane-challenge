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
        var controller = CreateController(repository, owner);

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

    [Fact]
    public async Task MissingGetReplaceDelete_ReturnIdentical404AndForwardBothIds()
    {
        var ownerId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var repository = new RecordingRepository();
        var controller = CreateController(repository, ownerId);

        var get = await controller.Get(taskId, default);
        var replace = await controller.Replace(
            taskId, new TaskWriteRequest("Task", null, "Pending", null), default);
        var delete = await controller.Delete(taskId, default);

        var responses = new ObjectResult[]
        {
            Assert.IsType<NotFoundObjectResult>(get.Result),
            Assert.IsType<NotFoundObjectResult>(replace.Result),
            Assert.IsType<NotFoundObjectResult>(delete)
        };
        Assert.All(responses, response =>
        {
            var problem = Assert.IsType<ProblemDetails>(response.Value);
            Assert.Equal(404, problem.Status);
            Assert.Equal("task.not_found", problem.Extensions["code"]);
        });
        Assert.Equal([(taskId, ownerId), (taskId, ownerId)], repository.GetRequests);
        Assert.Equal((taskId, ownerId), repository.DeleteRequest);
    }

    [Fact]
    public async Task Delete_ExistingTask_Returns204()
    {
        var repository = new RecordingRepository { DeleteResult = true };
        var controller = CreateController(repository, Guid.NewGuid());

        var result = await controller.Delete(Guid.NewGuid(), default);

        Assert.IsType<NoContentResult>(result);
    }

    private static TasksController CreateController(RecordingRepository repository, Guid ownerId)
    {
        var controller = new TasksController(
            new CreateTask(repository, new FixedDate()),
            new ListTasks(repository),
            new GetTask(repository),
            new ReplaceTask(repository, new FixedDate()),
            new DeleteTask(repository));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(JwtRegisteredClaimNames.Sub, ownerId.ToString())], "Bearer"))
            }
        };
        return controller;
    }

    private sealed class FixedDate : ICurrentDate
    {
        public DateOnly UtcToday => new(2026, 11, 6);
    }

    private sealed class RecordingRepository : ITaskRepository
    {
        public TaskItem? Added { get; private set; }
        public Guid ListOwner { get; private set; }
        public List<(Guid TaskId, Guid OwnerId)> GetRequests { get; } = [];
        public (Guid TaskId, Guid OwnerId) DeleteRequest { get; private set; }
        public bool DeleteResult { get; init; }
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
        public Task<TaskItem?> GetAsync(Guid id, Guid ownerId, CancellationToken cancellationToken)
        {
            GetRequests.Add((id, ownerId));
            return Task.FromResult<TaskItem?>(null);
        }
        public Task<bool> ReplaceAsync(TaskItem task, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> DeleteAsync(Guid id, Guid ownerId, CancellationToken cancellationToken)
        {
            DeleteRequest = (id, ownerId);
            return Task.FromResult(DeleteResult);
        }
    }
}
