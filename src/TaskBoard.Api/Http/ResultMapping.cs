using Microsoft.AspNetCore.Mvc;
using TaskBoard.Api.Contracts;
using TaskBoard.Application.Common;

namespace TaskBoard.Api.Http;

public static class ResultMapping
{
    public static ProblemDetails ToValidationProblem(this IReadOnlyList<AppError> errors) =>
        new()
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed",
            Extensions =
            {
                ["code"] = "validation.failed",
                ["errors"] = errors
                    .Select(error => new ApiError(error.Code, error.Field))
                    .ToArray()
            }
        };

    public static ProblemDetails InvalidRequest(IEnumerable<string> fields) =>
        new()
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Request could not be processed",
            Extensions =
            {
                ["code"] = "request.invalid",
                ["errors"] = fields
                    .Where(field => !string.IsNullOrWhiteSpace(field))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(field => new ApiError("request.invalid", NormalizeField(field)))
                    .ToArray()
            }
        };

    public static ProblemDetails RegistrationConflict() =>
        new()
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Registration could not be completed",
            Extensions = { ["code"] = "auth.registration.conflict" }
        };

    public static ProblemDetails Unauthorized(string code) =>
        new()
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Authentication required",
            Extensions = { ["code"] = code }
        };

    public static ProblemDetails TaskNotFound() =>
        new()
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Task not found",
            Extensions = { ["code"] = "task.not_found" }
        };

    private static string? NormalizeField(string field) => field switch
    {
        "$" => null,
        _ when field.StartsWith("$.", StringComparison.Ordinal) => field[2..],
        _ => field
    };
}
