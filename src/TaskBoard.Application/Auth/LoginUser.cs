using TaskBoard.Application.Abstractions;
using TaskBoard.Application.Common;

namespace TaskBoard.Application.Auth;

public sealed class LoginUser(
    IUserRepository repository,
    IPasswordHasher passwordHasher,
    IAccessTokenIssuer tokenIssuer)
{
    public async Task<Result<AccessTokenDto>> ExecuteAsync(
        LoginUserCommand command,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = command.Email.Trim().ToLowerInvariant();
        var user = await repository.GetByNormalizedEmailAsync(normalizedEmail, cancellationToken);
        var verified = passwordHasher.Verify(user?.PasswordHash, command.Password);

        if (user is null || !verified)
        {
            return Result<AccessTokenDto>.Failure(
                Outcome.Unauthorized,
                new AppError("auth.invalid_credentials"));
        }

        return Result<AccessTokenDto>.Success(tokenIssuer.Issue(user));
    }
}
