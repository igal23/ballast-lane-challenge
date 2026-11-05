using Microsoft.AspNetCore.Identity;
using TaskBoard.Application.Abstractions;

namespace TaskBoard.Infrastructure.Security;

public sealed class AspNetPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<object> _hasher = new();
    private readonly object _subject = new();
    private readonly string _dummyHash;

    public AspNetPasswordHasher()
    {
        _dummyHash = _hasher.HashPassword(_subject, "dummy password used for verification work");
    }

    public string Hash(string password) => _hasher.HashPassword(_subject, password);

    public bool Verify(string? hash, string suppliedPassword)
    {
        var result = _hasher.VerifyHashedPassword(
            _subject,
            hash ?? _dummyHash,
            suppliedPassword);

        return hash is not null && result != PasswordVerificationResult.Failed;
    }
}
