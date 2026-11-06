namespace TaskBoard.Application.Auth;

public sealed record RegisterUserCommand(string Email, string Password);

public sealed record UserDto(Guid Id, string Email);

public sealed record LoginUserCommand(string Email, string Password);

public sealed record AccessTokenDto(string AccessToken, DateTimeOffset ExpiresAt);
