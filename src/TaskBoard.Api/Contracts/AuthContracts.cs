namespace TaskBoard.Api.Contracts;

public sealed record RegisterRequest(string Email, string Password);

public sealed record RegisterResponse(Guid Id, string Email);

public sealed record ApiError(string Code, string? Field);

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt);

public sealed record UserResponse(Guid Id, string Email);
