namespace TaskBoard.Application.Common;

public sealed class Result<T>
{
    private Result(Outcome outcome, T? value, IReadOnlyList<AppError> errors)
    {
        Outcome = outcome;
        Value = value;
        Errors = errors;
    }

    public Outcome Outcome { get; }

    public T? Value { get; }

    public IReadOnlyList<AppError> Errors { get; }

    public static Result<T> Success(T value) =>
        new(Outcome.Success, value, Array.Empty<AppError>());

    public static Result<T> Failure(Outcome outcome, params AppError[] errors) =>
        new(outcome, default, errors);
}
