using TaskBoard.Application.Abstractions;
using TaskBoard.Application.Common;

namespace TaskBoard.Application.Auth;

public sealed class GetCurrentUser(IUserRepository repository)
{
    public async Task<Result<UserDto>> ExecuteAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await repository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result<UserDto>.Failure(
                Outcome.Unauthorized,
                new AppError("auth.unauthorized"));
        }

        return Result<UserDto>.Success(new UserDto(user.Id, user.Email));
    }
}
