# Generative AI Usage

## 1. AI Tool and Approach

**Tool:** OpenAI Codex with the Superpowers development workflow.

I used Codex as a development assistant to accelerate scaffolding, implementation, testing, and code review.

My approach was to define the architectural constraints and expected behavior first, then use AI to implement small, independently testable features.

Rather than generating the entire application in a single step, I followed an iterative process:

1. Define requirements and architecture.
2. Generate an implementation plan.
3. Implement features using Test-Driven Development (TDD).
4. Review the generated code and run automated tests.
5. Refactor and address issues identified during validation.

The objective was to use AI to accelerate development while maintaining control over architectural and technical decisions.

## 2. Prompt Used for API Generation

Use Superpowers brainstorming to design TaskBoard based on the repository requirements and existing decisions.
Focus on:
- Clean Architecture dependency boundaries
- Domain model, validation and meaningful business rules
- REST endpoints, JWT authentication and per-user task isolation
- PostgreSQL persistence using Npgsql directly
- TDD, unit tests, API and database integration tests
- Security, edge cases and local demo readiness
Keep the MVP small and maintainable. Do not reopen recorded technical choices without a concrete reason.
Create a concise design document under docs/ and update decisions only when needed.
Do not implement application code yet. Stop for my design approval.
```

## 3. Representative Generated Code

The following examples illustrate the intended implementation, including authorization, separation of concerns, and safe database access.

### API Controller

The controller handles HTTP requests while delegating business logic to the application layer.

```csharp
[ApiController]
[Authorize]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userId, out var ownerId))
            return Unauthorized();

        var task = await _taskService.GetByIdAsync(
            id, ownerId);

        return task is null ? NotFound() : Ok(task);
    }
}
```

### Data Access Layer

The repository uses Npgsql directly, without an ORM.

```csharp
public async Task<TaskItem?> GetByIdAsync(
    Guid taskId,
    Guid userId)
{
    const string sql = """
        SELECT id, title, description, status, due_date
        FROM tasks
        WHERE id = @taskId AND user_id = @userId
        """;

    await using var connection =
        await _dataSource.OpenConnectionAsync();

    await using var command =
        new NpgsqlCommand(sql, connection);

    command.Parameters.AddWithValue("taskId", taskId);
    command.Parameters.AddWithValue("userId", userId);

    await using var reader =
        await command.ExecuteReaderAsync();

    if (!await reader.ReadAsync())
        return null;

    return new TaskItem
    {
        Id = reader.GetGuid(0),
        Title = reader.GetString(1),
        Description = reader.IsDBNull(2)
            ? null
            : reader.GetString(2),
        Status = reader.GetString(3),
        DueDate = reader.IsDBNull(4)
            ? null
            : reader.GetFieldValue<DateTime>(4)
    };
}
```

These samples illustrate two important design decisions:

- **Separation of concerns:** controllers do not execute SQL or implement business rules.
- **Data isolation:** queries include the authenticated user's identifier, preventing access to another user's tasks.

The repository and service interfaces allow the application logic to be tested independently of PostgreSQL.

## 4. Validation of AI-Generated Code

AI-generated code should not be assumed correct simply because it compiles.

My validation approach focuses on four areas.

### Functional correctness

- Compare the implementation against the user story and acceptance criteria.
- Verify CRUD operations through actual HTTP requests.
- Check response payloads and HTTP status codes.
- Verify that database operations persist the expected changes.

### Automated testing

- **Unit tests:** verify domain rules and application behavior independently of infrastructure.
- **Integration tests:** verify repository behavior against PostgreSQL.
- **API tests:** verify routing, validation, authentication, authorization, and HTTP responses.

### Architecture review

- Verify that dependencies follow Clean Architecture boundaries.
- Check that business logic is not embedded in controllers or repositories.
- Review dependency injection and interface responsibilities.
- Identify unnecessary abstractions or duplicated logic.

### Security review

- Verify that SQL queries are parameterized.
- Confirm that passwords are securely hashed.
- Validate JWT authentication and protected endpoints.
- Verify that users cannot access or modify another user's tasks.

## 5. Corrections and Improvements

One of the most important aspects of AI-assisted development is challenging generated solutions rather than accepting them automatically.

The following are examples of improvements I would prioritize during review.

**User ownership enforcement**

A simple implementation might retrieve tasks using only their task ID. This is insufficient for a multi-user application.

I would require all task retrieval, update, and delete operations to enforce ownership using the authenticated user's ID.

**Avoiding unnecessary abstractions**

AI tools sometimes introduce generic repositories, additional service layers, or unnecessary design patterns.

For this application, I prefer explicit repositories and application services with clearly defined responsibilities.

**Database access**

I would replace any dynamically constructed SQL containing user input with parameterized queries and verify the handling of nullable values, missing records, and database constraints.

**Validation and error handling**

I would ensure validation failures return meaningful errors instead of unexpected server exceptions, using consistent HTTP responses such as `400 Bad Request`, `401 Unauthorized`, `404 Not Found`, and `409 Conflict`.

**Test quality**

I would review generated tests to ensure they validate meaningful behavior rather than implementation details. Mocks should be used for isolation, while integration tests should verify real database behavior.

## 6. Edge Cases, Authentication, and Validation

The following scenarios are particularly important:

| Scenario | Expected behavior |
|---|---|
| Create a task without authentication | 401 Unauthorized |
| Access another user's task | 404 Not Found |
| Create a task without a title | 400 Bad Request |
| Create a task with an invalid status | 400 Bad Request |
| Retrieve a nonexistent task | 404 Not Found |
| Register with an existing email | 409 Conflict |
| Login with invalid credentials | 401 Unauthorized |
| Create a valid task | 201 Created |
| Delete an existing task | 204 No Content |

Returning `404 Not Found` for another user's task avoids revealing whether that resource exists.

### Authentication

- Passwords are stored as secure hashes, never plaintext.
- Login credentials are validated before issuing a JWT.
- Protected endpoints require a valid authentication token.
- User ownership is determined from authenticated claims, not client-submitted user IDs.
- Authentication and authorization are treated as separate concerns.

### Input validation

- Required fields are validated.
- String lengths are constrained.
- Status values are restricted to supported values.
- Invalid identifiers and malformed requests are handled.
- Database constraints provide an additional layer of protection.

## 7. Conclusion

Generative AI was used as an engineering productivity tool, not as a replacement for technical judgment.

The main benefits were accelerating repetitive implementation, generating initial tests, exploring alternatives, and supporting code review.

The most important part of the process was maintaining control over architecture, security, correctness, and maintainability.

**My guiding principle is to treat AI-generated code as a proposal that must be understood, reviewed, tested, and validated before being accepted.**