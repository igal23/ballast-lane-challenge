using Microsoft.AspNetCore.Mvc;
using TaskBoard.Api.Contracts;
using TaskBoard.Api.Controllers;
using TaskBoard.Application.Abstractions;
using TaskBoard.Application.Auth;
using TaskBoard.Domain.Users;

namespace TaskBoard.UnitTests.Api;

public sealed class AuthControllerTests
{
    [Fact]
    public async Task Register_Success_Returns201WithPublicUserOnly()
    {
        var controller = CreateController(addResult: true);

        var action = await controller.Register(
            new RegisterRequest("user@example.com", new string('p', 15)),
            default);

        var created = Assert.IsType<CreatedResult>(action.Result);
        Assert.Equal(201, created.StatusCode);
        var response = Assert.IsType<RegisterResponse>(created.Value);
        Assert.Equal("user@example.com", response.Email);
        Assert.NotEqual(Guid.Empty, response.Id);
    }

    [Fact]
    public async Task Register_InvalidInput_Returns400ProblemDetailsWithStableError()
    {
        var controller = CreateController(addResult: true);

        var action = await controller.Register(
            new RegisterRequest("invalid", new string('p', 15)),
            default);

        var badRequest = Assert.IsType<BadRequestObjectResult>(action.Result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal(400, problem.Status);
        var errors = Assert.IsAssignableFrom<IReadOnlyList<ApiError>>(problem.Extensions["errors"]);
        Assert.Contains(errors, error => error.Code == "auth.email.invalid" && error.Field == "email");
    }

    [Fact]
    public async Task Register_Duplicate_ReturnsGeneric409ProblemDetails()
    {
        var controller = CreateController(addResult: false);

        var action = await controller.Register(
            new RegisterRequest("user@example.com", new string('p', 15)),
            default);

        var conflict = Assert.IsType<ConflictObjectResult>(action.Result);
        var problem = Assert.IsType<ProblemDetails>(conflict.Value);
        Assert.Equal(409, problem.Status);
        Assert.Equal("auth.registration.conflict", problem.Extensions["code"]);
        Assert.DoesNotContain("user@example.com", problem.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static AuthController CreateController(bool addResult)
    {
        var useCase = new RegisterUser(
            new StubUserRepository(addResult),
            new StubPasswordHasher());
        return new AuthController(useCase);
    }

    private sealed class StubUserRepository(bool addResult) : IUserRepository
    {
        public Task<UserAccount?> GetByNormalizedEmailAsync(
            string normalizedEmail,
            CancellationToken cancellationToken) => Task.FromResult<UserAccount?>(null);

        public Task<UserAccount?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken) => Task.FromResult<UserAccount?>(null);

        public Task<bool> AddAsync(UserAccount user, CancellationToken cancellationToken) =>
            Task.FromResult(addResult);
    }

    private sealed class StubPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => "hash";

        public bool Verify(string? hash, string suppliedPassword) => false;
    }
}
