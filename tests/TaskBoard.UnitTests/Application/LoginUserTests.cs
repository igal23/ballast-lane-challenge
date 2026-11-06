using TaskBoard.Application.Abstractions;
using TaskBoard.Application.Auth;
using TaskBoard.Application.Common;
using TaskBoard.Domain.Users;

namespace TaskBoard.UnitTests.Application;

public sealed class LoginUserTests
{
    [Fact]
    public async Task Execute_ValidCredentials_IssuesTokenForNormalizedEmail()
    {
        var user = NewUser();
        var repository = new StubRepository(user);
        var hasher = new RecordingHasher(true);
        var issuer = new RecordingIssuer();
        var useCase = new LoginUser(repository, hasher, issuer);

        var result = await useCase.ExecuteAsync(
            new LoginUserCommand("  USER@Example.COM ", "password"), default);

        Assert.Equal(Outcome.Success, result.Outcome);
        Assert.Equal("token", result.Value!.AccessToken);
        Assert.Equal("user@example.com", repository.RequestedEmail);
        Assert.Equal("hash", hasher.LastHash);
        Assert.Equal("password", hasher.Password);
        Assert.Same(user, issuer.User);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public async Task Execute_InvalidCredentials_IsGenericAndAlwaysVerifies(
        bool userExists,
        bool passwordMatches)
    {
        var repository = new StubRepository(userExists ? NewUser() : null);
        var hasher = new RecordingHasher(passwordMatches);
        var issuer = new RecordingIssuer();
        var useCase = new LoginUser(repository, hasher, issuer);

        var result = await useCase.ExecuteAsync(
            new LoginUserCommand("user@example.com", "wrong"), default);

        Assert.Equal(Outcome.Unauthorized, result.Outcome);
        Assert.Equal("auth.invalid_credentials", Assert.Single(result.Errors).Code);
        Assert.Equal(1, hasher.CallCount);
        Assert.Null(issuer.User);
        Assert.Null(result.Value);
    }

    private static UserAccount NewUser() =>
        new(Guid.NewGuid(), "user@example.com", "user@example.com", "hash");

    private sealed class StubRepository(UserAccount? user) : IUserRepository
    {
        public string? RequestedEmail { get; private set; }
        public Task<UserAccount?> GetByNormalizedEmailAsync(string email, CancellationToken cancellationToken)
        {
            RequestedEmail = email;
            return Task.FromResult(user);
        }
        public Task<UserAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<UserAccount?>(null);
        public Task<bool> AddAsync(UserAccount account, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class RecordingHasher(bool result) : IPasswordHasher
    {
        public int CallCount { get; private set; }
        public string? LastHash { get; private set; }
        public string? Password { get; private set; }
        public string Hash(string password) => throw new NotSupportedException();
        public bool Verify(string? hash, string password)
        {
            CallCount++;
            LastHash = hash;
            Password = password;
            return result;
        }
    }

    private sealed class RecordingIssuer : IAccessTokenIssuer
    {
        public UserAccount? User { get; private set; }
        public AccessTokenDto Issue(UserAccount user)
        {
            User = user;
            return new AccessTokenDto("token", DateTimeOffset.UtcNow.AddHours(1));
        }
    }
}
