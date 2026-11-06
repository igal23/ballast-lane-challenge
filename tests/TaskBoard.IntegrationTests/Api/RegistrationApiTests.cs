using System.Net;
using System.Net.Http.Json;
using TaskBoard.Api.Contracts;
using TaskBoard.IntegrationTests.Support;

namespace TaskBoard.IntegrationTests.Api;

public sealed class RegistrationApiTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _postgres;

    public RegistrationApiTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    [Fact]
    public async Task Register_ValidThenNormalizedDuplicate_Returns201ThenGeneric409()
    {
        await using var factory = new TaskBoardApiFactory(_postgres.ConnectionString);
        await factory.InitializeDatabaseAsync(default);
        using var client = factory.CreateClient();
        var email = $"User-{Guid.NewGuid():N}@Example.COM";
        var password = "a sufficiently long password";

        var created = await client.PostAsJsonAsync(
            "/api/auth/register",
            new { Email = $"  {email}  ", Password = password });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Null(created.Headers.Location);
        var createdBody = await created.Content.ReadAsStringAsync();
        var response = await created.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(response);
        Assert.Equal(email, response.Email);
        Assert.DoesNotContain(password, createdBody, StringComparison.Ordinal);
        Assert.DoesNotContain("hash", createdBody, StringComparison.OrdinalIgnoreCase);

        var duplicate = await client.PostAsJsonAsync(
            "/api/auth/register",
            new { Email = email.ToLowerInvariant(), Password = password });

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var duplicateBody = await duplicate.Content.ReadAsStringAsync();
        Assert.Contains("auth.registration.conflict", duplicateBody, StringComparison.Ordinal);
        Assert.DoesNotContain(email, duplicateBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(password, duplicateBody, StringComparison.Ordinal);
        Assert.DoesNotContain("hash", duplicateBody, StringComparison.OrdinalIgnoreCase);
    }
}
