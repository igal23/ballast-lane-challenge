using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskBoard.Api.Contracts;
using TaskBoard.Api.Http;
using TaskBoard.Application.Auth;
using TaskBoard.Application.Common;

namespace TaskBoard.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    RegisterUser registerUser,
    LoginUser loginUser,
    GetCurrentUser getCurrentUser) : ControllerBase
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

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await loginUser.ExecuteAsync(
            new LoginUserCommand(request.Email, request.Password),
            cancellationToken);

        return result.Outcome switch
        {
            Outcome.Success => Ok(new LoginResponse(
                result.Value!.AccessToken,
                result.Value.ExpiresAt)),
            Outcome.Unauthorized => Unauthorized(
                ResultMapping.Unauthorized("auth.invalid_credentials")),
            _ => throw new InvalidOperationException($"Unsupported outcome: {result.Outcome}")
        };
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> Me(CancellationToken cancellationToken)
    {
        if (!User.TryGetSubjectId(out var userId))
        {
            return Unauthorized(ResultMapping.Unauthorized("auth.unauthorized"));
        }

        var result = await getCurrentUser.ExecuteAsync(userId, cancellationToken);
        return result.Outcome switch
        {
            Outcome.Success => Ok(new UserResponse(result.Value!.Id, result.Value.Email)),
            Outcome.Unauthorized => Unauthorized(ResultMapping.Unauthorized("auth.unauthorized")),
            _ => throw new InvalidOperationException($"Unsupported outcome: {result.Outcome}")
        };
    }
}
