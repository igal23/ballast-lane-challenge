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
public sealed class TasksController(
    CreateTask createTask,
    ListTasks listTasks,
    GetTask getTask,
    ReplaceTask replaceTask,
    DeleteTask deleteTask) : ControllerBase
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

    [HttpGet("{id}")]
    public async Task<ActionResult<TaskResponse>> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetSubjectId(out var ownerId))
        {
            return Unauthorized(ResultMapping.Unauthorized("auth.unauthorized"));
        }

        var result = await getTask.ExecuteAsync(id, ownerId, cancellationToken);
        return result.Outcome switch
        {
            Outcome.Success => Ok(ToResponse(result.Value!)),
            Outcome.NotFound => NotFound(ResultMapping.TaskNotFound()),
            _ => throw new InvalidOperationException($"Unsupported outcome: {result.Outcome}")
        };
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TaskResponse>> Replace(
        Guid id,
        TaskWriteRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetSubjectId(out var ownerId))
        {
            return Unauthorized(ResultMapping.Unauthorized("auth.unauthorized"));
        }

        var result = await replaceTask.ExecuteAsync(
            id,
            ownerId,
            new TaskWriteCommand(request.Title, request.Description, request.Status, request.DueDate),
            cancellationToken);
        return result.Outcome switch
        {
            Outcome.Success => Ok(ToResponse(result.Value!)),
            Outcome.ValidationError => BadRequest(result.Errors.ToValidationProblem()),
            Outcome.NotFound => NotFound(ResultMapping.TaskNotFound()),
            _ => throw new InvalidOperationException($"Unsupported outcome: {result.Outcome}")
        };
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!User.TryGetSubjectId(out var ownerId))
        {
            return Unauthorized(ResultMapping.Unauthorized("auth.unauthorized"));
        }

        var result = await deleteTask.ExecuteAsync(id, ownerId, cancellationToken);
        return result.Outcome switch
        {
            Outcome.Success => NoContent(),
            Outcome.NotFound => NotFound(ResultMapping.TaskNotFound()),
            _ => throw new InvalidOperationException($"Unsupported outcome: {result.Outcome}")
        };
    }

    private static TaskResponse ToResponse(TaskDto task) =>
        new(task.Id, task.Title, task.Description, task.Status, task.DueDate);
}
