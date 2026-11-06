using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TaskBoard.Api.Contracts;
using TaskBoard.IntegrationTests.Support;

namespace TaskBoard.IntegrationTests.Api;

public sealed class AuthenticationApiTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _postgres;

    public AuthenticationApiTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    [Fact]
    public async Task LoginAndMe_ValidCredentials_ReturnCurrentUser()
    {
        await using var factory = new TaskBoardApiFactory(_postgres.ConnectionString);
        await factory.InitializeDatabaseAsync(default);
        using var client = factory.CreateClient();
        var email = $"auth-{Guid.NewGuid():N}@example.com";
        var password = "a sufficiently long password";
        var registration = await client.PostAsJsonAsync(
            "/api/auth/register", new RegisterRequest(email, password));
        Assert.True(
            registration.IsSuccessStatusCode,
            await registration.Content.ReadAsStringAsync());
        var registered = await registration.Content.ReadFromJsonAsync<RegisterResponse>();

        var login = await client.PostAsJsonAsync(
            "/api/auth/login", new LoginRequest(email, password));

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var token = await login.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(token);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token.AccessToken);

        var me = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        var current = await me.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal(registered!.Id, current!.Id);
        Assert.Equal(email, current.Email);
    }

    [Fact]
    public async Task Login_MissingUserAndWrongPassword_ReturnSameGeneric401Body()
    {
        await using var factory = new TaskBoardApiFactory(_postgres.ConnectionString);
        await factory.InitializeDatabaseAsync(default);
        using var client = factory.CreateClient();
        var email = $"auth-{Guid.NewGuid():N}@example.com";
        var password = "a sufficiently long password";
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password));

        var wrongPassword = await client.PostAsJsonAsync(
            "/api/auth/login", new LoginRequest(email, "wrong but long enough"));
        var missingUser = await client.PostAsJsonAsync(
            "/api/auth/login", new LoginRequest($"missing-{email}", "wrong but long enough"));

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, missingUser.StatusCode);
        Assert.Equal(
            await wrongPassword.Content.ReadAsStringAsync(),
            await missingUser.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Me_InvalidBearerVariants_Return401()
    {
        await using var factory = new TaskBoardApiFactory(_postgres.ConnectionString);
        using var client = factory.CreateClient();
        var expired = CreateToken(
            TaskBoardApiFactory.JwtSigningKey,
            DateTime.UtcNow.AddMinutes(-1));
        var wrongSignature = CreateToken(
            "wrong-signing-key-that-is-at-least-32-bytes",
            DateTime.UtcNow.AddMinutes(10));
        string?[] tokens = [null, "not-a-jwt", wrongSignature, expired];

        foreach (var token in tokens)
        {
            client.DefaultRequestHeaders.Authorization = token is null
                ? null
                : new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("/api/auth/me");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    private static string CreateToken(string signingKey, DateTime expires)
    {
        var token = new JwtSecurityToken(
            issuer: TaskBoardApiFactory.JwtIssuer,
            audience: TaskBoardApiFactory.JwtAudience,
            claims: [new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString())],
            notBefore: DateTime.UtcNow.AddHours(-2),
            expires: expires,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
