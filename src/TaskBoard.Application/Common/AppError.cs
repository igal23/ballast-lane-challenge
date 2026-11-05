namespace TaskBoard.Application.Common;

public sealed record AppError(string Code, string? Field = null);
