using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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

    [Theory]
    [InlineData(true, true, 200)]
    [InlineData(true, false, 401)]
    [InlineData(false, false, 401)]
    public async Task Login_MapsSuccessAndGenericUnauthorized(
        bool userExists,
        bool passwordMatches,
        int expectedStatus)
    {
        var user = userExists ? NewUser() : null;
        var controller = CreateController(true, user, passwordMatches);

        var action = await controller.Login(
            new LoginRequest("user@example.com", "password"), default);

        var result = Assert.IsAssignableFrom<ObjectResult>(action.Result);
        Assert.Equal(expectedStatus, result.StatusCode);
        if (expectedStatus == 200)
        {
            Assert.IsType<LoginResponse>(result.Value);
        }
        else
        {
            var problem = Assert.IsType<ProblemDetails>(result.Value);
            Assert.Equal("auth.invalid_credentials", problem.Extensions["code"]);
        }
    }

    [Fact]
    public async Task Me_UsesOnlyAuthenticatedSubjectId()
    {
        var user = NewUser();
        var repository = new StubUserRepository(true, user);
        var controller = CreateController(repository, passwordMatches: true);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, "attacker@example.com")
                ], "Bearer"))
            }
        };

        var action = await controller.Me(default);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        Assert.Equal(user.Id, Assert.IsType<UserResponse>(ok.Value).Id);
        Assert.Equal(user.Id, repository.RequestedId);
    }

    private static AuthController CreateController(
        bool addResult,
        UserAccount? user = null,
        bool passwordMatches = false) =>
        CreateController(new StubUserRepository(addResult, user), passwordMatches);

    private static AuthController CreateController(
        StubUserRepository repository,
        bool passwordMatches)
    {
        var hasher = new StubPasswordHasher(passwordMatches);
        return new AuthController(
            new RegisterUser(repository, hasher),
            new LoginUser(repository, hasher, new StubTokenIssuer()),
            new GetCurrentUser(repository));
    }

    private static UserAccount NewUser() =>
        new(Guid.NewGuid(), "user@example.com", "user@example.com", "hash");

    private sealed class StubUserRepository(bool addResult, UserAccount? user = null) : IUserRepository
    {
        public Guid? RequestedId { get; private set; }

        public Task<UserAccount?> GetByNormalizedEmailAsync(
            string normalizedEmail,
            CancellationToken cancellationToken) => Task.FromResult(user);

        public Task<UserAccount?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            RequestedId = id;
            return Task.FromResult(user);
        }

        public Task<bool> AddAsync(UserAccount user, CancellationToken cancellationToken) =>
            Task.FromResult(addResult);
    }

    private sealed class StubPasswordHasher(bool verifyResult) : IPasswordHasher
    {
        public string Hash(string password) => "hash";

        public bool Verify(string? hash, string suppliedPassword) => verifyResult;
    }

    private sealed class StubTokenIssuer : IAccessTokenIssuer
    {
        public AccessTokenDto Issue(UserAccount user) =>
            new("token", DateTimeOffset.UtcNow.AddHours(1));
    }
}
