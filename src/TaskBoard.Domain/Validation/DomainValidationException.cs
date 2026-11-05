namespace TaskBoard.Domain.Validation;

public sealed class DomainValidationException : Exception
{
    public DomainValidationException(string code, string field)
        : base(code)
    {
        Code = code;
        Field = field;
    }

    public string Code { get; }

    public string Field { get; }
}
