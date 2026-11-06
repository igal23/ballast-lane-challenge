using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using TaskBoard.Api.Contracts;
using TaskBoard.IntegrationTests.Support;

namespace TaskBoard.IntegrationTests.Api;

public sealed class ErrorContractApiTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _postgres;

    public ErrorContractApiTests(PostgresFixture postgres) => _postgres = postgres;

    [Fact]
    public async Task InvalidBearer_ReturnsStableAuthenticationProblemDetails()
    {
        await using var factory = new TaskBoardApiFactory(_postgres.ConnectionString);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "not-a-jwt");

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal("Bearer", Assert.Single(response.Headers.WwwAuthenticate).Scheme);
        await AssertProblem(response, HttpStatusCode.Unauthorized, "auth.unauthorized");
    }

    [Fact]
    public async Task FrameworkRejectedRequests_ReturnStableRequestProblemDetails()
    {
        await using var factory = new TaskBoardApiFactory(_postgres.ConnectionString);
        await factory.InitializeDatabaseAsync(default);
        using var client = factory.CreateClient();
        await Authenticate(client);

        var malformedId = await client.GetAsync("/api/tasks/not-a-guid");
        await AssertProblem(malformedId, HttpStatusCode.BadRequest, "request.invalid");

        using var malformedJson = new StringContent("{", Encoding.UTF8, "application/json");
        var malformedBody = await client.PostAsync("/api/tasks", malformedJson);
        await AssertProblem(malformedBody, HttpStatusCode.BadRequest, "request.invalid");

        client.DefaultRequestHeaders.Authorization = null;
        var nullRequiredField = await client.PostAsJsonAsync(
            "/api/auth/register",
            new { email = (string?)null, password = "a sufficiently long password" });
        await AssertProblem(nullRequiredField, HttpStatusCode.BadRequest, "request.invalid");
    }

    private static async Task Authenticate(HttpClient client)
    {
        var email = $"errors-{Guid.NewGuid():N}@example.com";
        const string password = "a sufficiently long password";
        var registration = await client.PostAsJsonAsync(
            "/api/auth/register", new RegisterRequest(email, password));
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
        var login = await client.PostAsJsonAsync(
            "/api/auth/login", new LoginRequest(email, password));
        var token = await login.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token!.AccessToken);
    }

    private static async Task AssertProblem(
        HttpResponseMessage response,
        HttpStatusCode status,
        string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal((int)status, document.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(code, document.RootElement.GetProperty("code").GetString());
    }
}
