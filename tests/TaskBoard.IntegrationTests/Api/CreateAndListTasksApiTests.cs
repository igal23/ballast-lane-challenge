using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TaskBoard.Api.Contracts;
using TaskBoard.IntegrationTests.Support;

namespace TaskBoard.IntegrationTests.Api;

public sealed class CreateAndListTasksApiTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _postgres;
    public CreateAndListTasksApiTests(PostgresFixture postgres) => _postgres = postgres;

    [Fact]
    public async Task CreateAndList_EnforceAuthenticationValidationAndOwnerFreeBodies()
    {
        await using var factory = new TaskBoardApiFactory(_postgres.ConnectionString);
        await factory.InitializeDatabaseAsync(default);
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/tasks")).StatusCode);
        var email = $"tasks-{Guid.NewGuid():N}@example.com";
        var password = "a sufficiently long password";
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password));
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        var token = await login.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);

        var invalid = await client.PostAsJsonAsync(
            "/api/tasks", new TaskWriteRequest("Task", null, "pending", null));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Contains("task.status.invalid", await invalid.Content.ReadAsStringAsync());

        var created = await client.PostAsJsonAsync(
            "/api/tasks", new TaskWriteRequest(" Task ", " Description ", "Pending", DateOnly.FromDateTime(DateTime.UtcNow)));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.NotNull(created.Headers.Location);
        var createdJson = await created.Content.ReadAsStringAsync();
        Assert.DoesNotContain("owner", createdJson, StringComparison.OrdinalIgnoreCase);

        var listed = await client.GetAsync("/api/tasks");
        Assert.Equal(HttpStatusCode.OK, listed.StatusCode);
        var listedJson = await listed.Content.ReadAsStringAsync();
        Assert.Contains("Task", listedJson);
        Assert.DoesNotContain("owner", listedJson, StringComparison.OrdinalIgnoreCase);
    }
}
