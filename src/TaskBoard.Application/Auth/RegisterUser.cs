using System.ComponentModel.DataAnnotations;
using TaskBoard.Application.Abstractions;
using TaskBoard.Application.Common;
using TaskBoard.Domain.Users;

namespace TaskBoard.Application.Auth;

public sealed class RegisterUser(
    IUserRepository repository,
    IPasswordHasher passwordHasher)
{
    public async Task<Result<UserDto>> ExecuteAsync(
        RegisterUserCommand command,
        CancellationToken cancellationToken)
    {
        var email = command.Email?.Trim();

        if (email is null)
        {
            return ValidationError("auth.email.invalid", "email");
        }

        if (email.Length > 254)
        {
            return ValidationError("auth.email.too_long", "email");
        }

        if (!new EmailAddressAttribute().IsValid(email))
        {
            return ValidationError("auth.email.invalid", "email");
        }

        if (command.Password is null || command.Password.Length is < 15 or > 128)
        {
            return ValidationError("auth.password.length", "password");
        }

        var user = new UserAccount(
            Guid.NewGuid(),
            email,
            email.ToLowerInvariant(),
            passwordHasher.Hash(command.Password));

        var added = await repository.AddAsync(user, cancellationToken);
        if (!added)
        {
            return Result<UserDto>.Failure(
                Outcome.Conflict,
                new AppError("auth.registration.conflict"));
        }

        return Result<UserDto>.Success(new UserDto(user.Id, user.Email));
    }

    private static Result<UserDto> ValidationError(string code, string field) =>
        Result<UserDto>.Failure(
            Outcome.ValidationError,
            new AppError(code, field));
}
