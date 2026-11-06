using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Npgsql;
using TaskBoard.Api.Contracts;
using TaskBoard.IntegrationTests.Support;

namespace TaskBoard.IntegrationTests.Api;

public sealed class TaskOwnershipApiTests : IClassFixture<PostgresFixture>
{
    private static readonly DateTimeOffset FixedNow =
        new(2026, 11, 6, 12, 0, 0, TimeSpan.Zero);
    private readonly PostgresFixture _postgres;
    public TaskOwnershipApiTests(PostgresFixture postgres) => _postgres = postgres;

    [Fact]
    public async Task Crud_EnforcesOwnerIsolationAndPersistentDeletion()
    {
        await using var factory = new TaskBoardApiFactory(_postgres.ConnectionString, FixedNow);
        await factory.InitializeDatabaseAsync(default);
        using var ownerClient = factory.CreateClient();
        using var otherClient = factory.CreateClient();
        await Authenticate(ownerClient, "owner");
        await Authenticate(otherClient, "other");
        var created = await ownerClient.PostAsJsonAsync(
            "/api/tasks", new TaskWriteRequest("Original", null, "Pending", null));
        var task = await created.Content.ReadFromJsonAsync<TaskResponse>();
        var replacement = new TaskWriteRequest("Updated", "Saved", "Wip", null);

        Assert.Equal(HttpStatusCode.OK, (await ownerClient.GetAsync($"/api/tasks/{task!.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await ownerClient.PutAsJsonAsync($"/api/tasks/{task.Id}", replacement)).StatusCode);

        var missingId = Guid.NewGuid();
        await AssertSameNotFound(
            await otherClient.GetAsync($"/api/tasks/{task.Id}"),
            await otherClient.GetAsync($"/api/tasks/{missingId}"));
        await AssertSameNotFound(
            await otherClient.PutAsJsonAsync($"/api/tasks/{task.Id}", replacement),
            await otherClient.PutAsJsonAsync($"/api/tasks/{missingId}", replacement));
        await AssertSameNotFound(
            await otherClient.DeleteAsync($"/api/tasks/{task.Id}"),
            await otherClient.DeleteAsync($"/api/tasks/{missingId}"));
        Assert.Equal(HttpStatusCode.BadRequest, (await ownerClient.GetAsync("/api/tasks/not-a-guid")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await ownerClient.DeleteAsync($"/api/tasks/{task.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await ownerClient.GetAsync($"/api/tasks/{task.Id}")).StatusCode);
    }

    [Fact]
    public async Task Replace_OverdueDateMayRemainButCannotChangeToAnotherPastDate()
    {
        await using var factory = new TaskBoardApiFactory(_postgres.ConnectionString, FixedNow);
        await factory.InitializeDatabaseAsync(default);
        using var client = factory.CreateClient();
        var user = await Authenticate(client, "overdue");
        var taskId = Guid.NewGuid();
        await InsertTask(user.Id, taskId, new DateOnly(2026, 11, 5));

        var unchanged = await client.PutAsJsonAsync(
            $"/api/tasks/{taskId}",
            new TaskWriteRequest("Changed", "Saved", "Completed", new DateOnly(2026, 11, 5)));
        Assert.Equal(HttpStatusCode.OK, unchanged.StatusCode);

        var invalid = await client.PutAsJsonAsync(
            $"/api/tasks/{taskId}",
            new TaskWriteRequest("Must not persist", null, "Pending", new DateOnly(2026, 11, 4)));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        var saved = await client.GetFromJsonAsync<TaskResponse>($"/api/tasks/{taskId}");
        Assert.Equal("Changed", saved!.Title);
        Assert.Equal("Saved", saved.Description);
        Assert.Equal("Completed", saved.Status);
        Assert.Equal(new DateOnly(2026, 11, 5), saved.DueDate);
    }

    private static async Task<RegisterResponse> Authenticate(HttpClient client, string prefix)
    {
        var email = $"{prefix}-{Guid.NewGuid():N}@example.com";
        const string password = "a sufficiently long password";
        var registration = await client.PostAsJsonAsync(
            "/api/auth/register", new RegisterRequest(email, password));
        Assert.True(registration.IsSuccessStatusCode, await registration.Content.ReadAsStringAsync());
        var user = (await registration.Content.ReadFromJsonAsync<RegisterResponse>())!;
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        Assert.True(login.IsSuccessStatusCode, await login.Content.ReadAsStringAsync());
        var token = (await login.Content.ReadFromJsonAsync<LoginResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        return user;
    }

    private static async Task AssertSameNotFound(HttpResponseMessage foreign, HttpResponseMessage missing)
    {
        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(await foreign.Content.ReadAsStringAsync(), await missing.Content.ReadAsStringAsync());
    }

    private async Task InsertTask(Guid ownerId, Guid taskId, DateOnly dueDate)
    {
        await using var dataSource = NpgsqlDataSource.Create(_postgres.ConnectionString);
        await using var command = dataSource.CreateCommand(
            """
            INSERT INTO tasks(id, owner_id, title, description, status, due_date)
            VALUES (@id, @owner_id, 'Overdue', NULL, 'Pending', @due_date);
            """);
        command.Parameters.AddWithValue("id", taskId);
        command.Parameters.AddWithValue("owner_id", ownerId);
        command.Parameters.AddWithValue("due_date", dueDate);
        await command.ExecuteNonQueryAsync();
    }
}
