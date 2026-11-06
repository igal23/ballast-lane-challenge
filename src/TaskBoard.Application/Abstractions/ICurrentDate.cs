namespace TaskBoard.Application.Abstractions;

public interface ICurrentDate
{
    DateOnly UtcToday { get; }
}
