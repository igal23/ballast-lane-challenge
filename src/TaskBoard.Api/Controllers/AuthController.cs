using Microsoft.AspNetCore.Mvc;
using TaskBoard.Api.Contracts;
using TaskBoard.Api.Http;
using TaskBoard.Application.Auth;
using TaskBoard.Application.Common;

namespace TaskBoard.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(RegisterUser registerUser) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await registerUser.ExecuteAsync(
            new RegisterUserCommand(request.Email, request.Password),
            cancellationToken);

        return result.Outcome switch
        {
            Outcome.Success => Created(
                $"/api/users/{result.Value!.Id}",
                new RegisterResponse(result.Value.Id, result.Value.Email)),
            Outcome.ValidationError => BadRequest(result.Errors.ToValidationProblem()),
            Outcome.Conflict => Conflict(ResultMapping.RegistrationConflict()),
            _ => throw new InvalidOperationException($"Unsupported outcome: {result.Outcome}")
        };
    }
}
