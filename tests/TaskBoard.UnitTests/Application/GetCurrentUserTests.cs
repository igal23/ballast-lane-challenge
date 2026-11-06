using TaskBoard.Application.Abstractions;
using TaskBoard.Application.Auth;
using TaskBoard.Application.Common;
using TaskBoard.Domain.Users;

namespace TaskBoard.UnitTests.Application;

public sealed class GetCurrentUserTests
{
    [Fact]
    public async Task Execute_ExistingSubject_ReturnsPublicUser()
    {
        var id = Guid.NewGuid();
        var repository = new StubRepository(new UserAccount(id, "user@example.com", "user@example.com", "secret"));

        var result = await new GetCurrentUser(repository).ExecuteAsync(id, default);

        Assert.Equal(Outcome.Success, result.Outcome);
        Assert.Equal(id, repository.RequestedId);
        Assert.Equal(new UserDto(id, "user@example.com"), result.Value);
    }

    private sealed class StubRepository(UserAccount user) : IUserRepository
    {
        public Guid RequestedId { get; private set; }
        public Task<UserAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            RequestedId = id;
            return Task.FromResult<UserAccount?>(user);
        }
        public Task<UserAccount?> GetByNormalizedEmailAsync(string email, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> AddAsync(UserAccount account, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
