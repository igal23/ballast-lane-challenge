using TaskBoard.Application.Abstractions;
using TaskBoard.Application.Auth;
using TaskBoard.Application.Common;
using TaskBoard.Domain.Users;

namespace TaskBoard.UnitTests.Application;

public sealed class RegisterUserTests
{
    public static TheoryData<string?, string?, string, string> InvalidRegistrations => new()
    {
        { null, new string('p', 15), "auth.email.invalid", "email" },
        { "not-an-email", new string('p', 15), "auth.email.invalid", "email" },
        { $"{new string('x', 243)}@example.com", new string('p', 15), "auth.email.too_long", "email" },
        { "user@example.com", null, "auth.password.length", "password" },
        { "user@example.com", new string('p', 14), "auth.password.length", "password" },
        { "user@example.com", new string('p', 129), "auth.password.length", "password" }
    };

    public static TheoryData<string> BoundaryPasswords => new()
    {
        $" {new string('é', 13)} ",
        $" {new string('é', 126)} "
    };

    [Theory]
    [MemberData(nameof(InvalidRegistrations))]
    public async Task Execute_InvalidRegistration_ReturnsFieldErrorWithoutPersistence(
        string? email,
        string? password,
        string expectedCode,
        string expectedField)
    {
        var repository = new RecordingUserRepository();
        var hasher = new RecordingPasswordHasher();
        var useCase = new RegisterUser(repository, hasher);

        var result = await useCase.ExecuteAsync(
            new RegisterUserCommand(email!, password!),
            default);

        Assert.Equal(Outcome.ValidationError, result.Outcome);
        var error = Assert.Single(result.Errors);
        Assert.Equal(expectedCode, error.Code);
        Assert.Equal(expectedField, error.Field);
        Assert.Equal(0, repository.AddCallCount);
        Assert.Null(hasher.LastHashedPassword);
    }

    [Fact]
    public async Task Execute_ValidRegistration_NormalizesEmailAndStoresOnlyHash()
    {
        var repository = new RecordingUserRepository();
        var hasher = new RecordingPasswordHasher();
        var useCase = new RegisterUser(repository, hasher);
        const string password = " passphrase éé ";

        var result = await useCase.ExecuteAsync(
            new RegisterUserCommand("  User@Example.COM  ", password),
            default);

        Assert.Equal(Outcome.Success, result.Outcome);
        Assert.Equal("User@Example.COM", result.Value!.Email);
        Assert.Equal(password, hasher.LastHashedPassword);
        Assert.Equal("HASHED", repository.AddedUser!.PasswordHash);
        Assert.Equal("user@example.com", repository.AddedUser.NormalizedEmail);
        Assert.Equal("User@Example.COM", repository.AddedUser.Email);
    }

    [Theory]
    [MemberData(nameof(BoundaryPasswords))]
    public async Task Execute_BoundaryPassword_IsHashedWithoutTrimming(string password)
    {
        var repository = new RecordingUserRepository();
        var hasher = new RecordingPasswordHasher();
        var useCase = new RegisterUser(repository, hasher);

        var result = await useCase.ExecuteAsync(
            new RegisterUserCommand("user@example.com", password),
            default);

        Assert.Equal(Outcome.Success, result.Outcome);
        Assert.Equal(password, hasher.LastHashedPassword);
    }

    [Fact]
    public async Task Execute_DuplicateEmail_ReturnsConflictWithoutExposingPersistenceDetails()
    {
        var repository = new RecordingUserRepository { AddResult = false };
        var useCase = new RegisterUser(repository, new RecordingPasswordHasher());

        var result = await useCase.ExecuteAsync(
            new RegisterUserCommand("user@example.com", new string('p', 15)),
            default);

        Assert.Equal(Outcome.Conflict, result.Outcome);
        var error = Assert.Single(result.Errors);
        Assert.Equal("auth.registration.conflict", error.Code);
        Assert.Null(error.Field);
        Assert.Null(result.Value);
    }

    private sealed class RecordingUserRepository : IUserRepository
    {
        public int AddCallCount { get; private set; }

        public UserAccount? AddedUser { get; private set; }

        public bool AddResult { get; init; } = true;

        public Task<UserAccount?> GetByNormalizedEmailAsync(
            string normalizedEmail,
            CancellationToken cancellationToken) => Task.FromResult<UserAccount?>(null);

        public Task<UserAccount?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken) => Task.FromResult<UserAccount?>(null);

        public Task<bool> AddAsync(UserAccount user, CancellationToken cancellationToken)
        {
            AddCallCount++;
            AddedUser = user;
            return Task.FromResult(AddResult);
        }
    }

    private sealed class RecordingPasswordHasher : IPasswordHasher
    {
        public string? LastHashedPassword { get; private set; }

        public string Hash(string password)
        {
            LastHashedPassword = password;
            return "HASHED";
        }

        public bool Verify(string? hash, string suppliedPassword) => false;
    }
}
