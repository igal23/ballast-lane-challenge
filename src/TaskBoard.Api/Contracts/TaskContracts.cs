namespace TaskBoard.Api.Contracts;

public sealed record TaskWriteRequest(
    string Title,
    string? Description,
    string Status,
    DateOnly? DueDate);

public sealed record TaskResponse(
    Guid Id,
    string Title,
    string? Description,
    string Status,
    DateOnly? DueDate);
