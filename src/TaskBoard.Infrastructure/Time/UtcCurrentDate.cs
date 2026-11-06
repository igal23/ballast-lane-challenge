using TaskBoard.Application.Abstractions;

namespace TaskBoard.Infrastructure.Time;

public sealed class UtcCurrentDate(TimeProvider timeProvider) : ICurrentDate
{
    public DateOnly UtcToday => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
}
