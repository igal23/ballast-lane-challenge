using TaskBoard.Domain.Users;

namespace TaskBoard.Application.Abstractions;

public interface IUserRepository
{
    Task<UserAccount?> GetByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken);

    Task<UserAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> AddAsync(UserAccount user, CancellationToken cancellationToken);
}
