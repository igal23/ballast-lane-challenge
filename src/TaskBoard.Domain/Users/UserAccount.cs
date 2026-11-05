namespace TaskBoard.Domain.Users;

public sealed record UserAccount(
    Guid Id,
    string Email,
    string NormalizedEmail,
    string PasswordHash);
