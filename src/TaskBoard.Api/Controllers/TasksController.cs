using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskBoard.Api.Contracts;
using TaskBoard.Api.Http;
using TaskBoard.Application.Common;
using TaskBoard.Application.Tasks;

namespace TaskBoard.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tasks")]
public sealed class TasksController(CreateTask createTask, ListTasks listTasks) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<TaskResponse>> Create(
        TaskWriteRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetSubjectId(out var ownerId))
        {
            return Unauthorized(ResultMapping.Unauthorized("auth.unauthorized"));
        }

        var result = await createTask.ExecuteAsync(
            ownerId,
            new TaskWriteCommand(request.Title, request.Description, request.Status, request.DueDate),
            cancellationToken);
        return result.Outcome switch
        {
            Outcome.Success => Created(
                $"/api/tasks/{result.Value!.Id}",
                ToResponse(result.Value)),
            Outcome.ValidationError => BadRequest(result.Errors.ToValidationProblem()),
            _ => throw new InvalidOperationException($"Unsupported outcome: {result.Outcome}")
        };
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TaskResponse>>> List(
        CancellationToken cancellationToken)
    {
        if (!User.TryGetSubjectId(out var ownerId))
        {
            return Unauthorized(ResultMapping.Unauthorized("auth.unauthorized"));
        }

        var result = await listTasks.ExecuteAsync(ownerId, cancellationToken);
        return Ok(result.Value!.Select(ToResponse).ToArray());
    }

    private static TaskResponse ToResponse(TaskDto task) =>
        new(task.Id, task.Title, task.Description, task.Status, task.DueDate);
}
