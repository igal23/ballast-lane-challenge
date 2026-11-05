namespace TaskBoard.Application.Auth;

public sealed record RegisterUserCommand(string Email, string Password);

public sealed record UserDto(Guid Id, string Email);
