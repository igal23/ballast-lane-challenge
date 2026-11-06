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
                ["errors"] = errors
                    .Select(error => new ApiError(error.Code, error.Field))
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
}
