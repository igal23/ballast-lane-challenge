# Generative AI Usage

## Tool and working method

**Tool:** OpenAI Codex with the Superpowers development workflow.

I used Codex to help design, plan, implement, test, review, and document TaskBoard. I did not ask it to generate the application in one unreviewed pass. The workflow was:

1. Establish requirements and technical constraints.
2. Review and approve the design before implementation.
3. Break the implementation into small vertical slices.
4. Apply RED-GREEN TDD to business behavior and regressions.
5. Commit each task only after its tests passed.
6. Run unit, API, PostgreSQL integration, frontend, build, and browser checks.
7. Correct generated assumptions when evidence showed they were wrong.

## Prompts used

The initial design prompt was:

```text
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

After design approval, the implementation-planning prompt was:

```text
Use Superpowers writing-plans to create an implementation plan based on the approved design.

Break the work into small, independently testable vertical slices, following TDD.
```

The execution constraints were:

```text
lets do native, but use superpowers:test-driven-development. Red-Green flow
for each task, do a commit
commit only when the task is verified with a test
```

This staged prompt sequence generated the implementation through reviewed, testable slices rather than treating AI output as authoritative.

## Representative generated code

### Controller delegates to an explicit use case

The actual task controller obtains the owner only from the authenticated subject and delegates creation to the Application layer:

```csharp
[ApiController]
[Authorize]
[Route("api/tasks")]
public sealed class TasksController(
    CreateTask createTask,
    ListTasks listTasks,
    GetTask getTask,
    ReplaceTask replaceTask,
    DeleteTask deleteTask) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<TaskResponse>> Create(
        TaskWriteRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetSubjectId(out var ownerId))
        {
            return Unauthorized(ResultMapping.Unauthorized("auth.unauthorized"));
        }

        var result = await createTask.ExecuteAsync(
            ownerId,
            new TaskWriteCommand(request.Title, request.Description, request.Status, request.DueDate),
            cancellationToken);

        return result.Outcome switch
        {
            Outcome.Success => Created(
                $"/api/tasks/{result.Value!.Id}",
                ToResponse(result.Value)),
            Outcome.ValidationError => BadRequest(result.Errors.ToValidationProblem()),
            _ => throw new InvalidOperationException($"Unsupported outcome: {result.Outcome}")
        };
    }
}
```

### Npgsql repository enforces ownership and parameterizes values

The actual repository uses the schema's `owner_id` column and typed parameters:

```csharp
public async Task<TaskItem?> GetAsync(
    Guid id,
    Guid ownerId,
    CancellationToken cancellationToken)
{
    await using var command = dataSource.CreateCommand(
        """
        SELECT id, owner_id, title, description, status, due_date
        FROM tasks
        WHERE id = @id AND owner_id = @owner_id;
        """);
    command.Parameters.Add("id", NpgsqlDbType.Uuid).Value = id;
    command.Parameters.Add("owner_id", NpgsqlDbType.Uuid).Value = ownerId;

    await using var reader = await command.ExecuteReaderAsync(cancellationToken);
    return await reader.ReadAsync(cancellationToken) ? ReadTask(reader) : null;
}
```

### Domain rule discovered during design

The generated replacement behavior allows an existing overdue date to remain unchanged but rejects selecting a different past date:

```csharp
if (dueDate != DueDate)
{
    ValidateDueDate(dueDate, utcToday);
}
```

## How the AI output was validated and corrected

| Generated proposal or assumption | Validation | Correction |
| --- | --- | --- |
| JWT configuration could be captured eagerly during host composition | A real `WebApplicationFactory` authentication test returned `500` | Bound bearer options after final configuration so test overrides and runtime configuration use the same effective values |
| A fixed test clock automatically controlled IdentityModel lifetime validation | Expired-token tests consulted a different clock | Added an explicit lifetime validator using the injected `TimeProvider` while retaining issuer, audience, signature, and skew validation |
| Default ASP.NET `400` and JWT `401` responses were sufficient | API contract tests exposed inconsistent response bodies | Added stable Problem Details error codes for request rejection and authentication challenges |
| A public rehydration method could trust persisted values | Domain review found invariant bypasses | Added RED-GREEN tests and validation for identifiers, text limits, and status while preserving stored text and overdue dates |
| Mobile account navigation required no special handling | Browser inspection at 390 px showed wrapping | Added a failing computed-style test, then applied `white-space: nowrap` |
| A flat task list was sufficient for scanning | User review requested grouping by status | Added native tracked accordions, component regressions, and desktop/mobile browser verification |

## Authentication, validation, and edge cases

- Passwords are stored through ASP.NET Core's adaptive password hasher, never as plaintext.
- JWTs validate issuer, audience, signature, and lifetime.
- Task routes use the authenticated subject and never accept an owner ID from the client.
- Owner predicates apply to read, replace, and delete SQL; foreign and missing records both return `404`.
- All user-influenced SQL values use Npgsql parameters.
- Registration normalizes email and handles concurrent duplicates deterministically.
- Domain and database constraints cover blank or oversized task text and invalid status values.
- Due dates are optional; newly selected dates must be today or later using the server's UTC date.
- Malformed requests, invalid credentials, invalid JWTs, unknown IDs, and persistence conflicts have stable HTTP behavior.

## Verification evidence

Latest verified results:

| Check | Result |
| --- | --- |
| `dotnet test TaskBoard.slnx` | 60 unit tests and 36 integration tests passed |
| `npm test -- --watch=false` | 31 Angular tests passed across 9 files |
| `npm run build` | Production build completed; initial bundle 266.01 kB, estimated transfer 72.54 kB |
| PostgreSQL integration | Real PostgreSQL exercised through Testcontainers, including migrations, constraints, repositories, seeding, and hosted API behavior |
| Browser inspection | Authenticated CRUD and status accordions inspected at desktop and 390 by 844; browser warning/error console was empty |

More detailed prompt, correction, security, and commit evidence is recorded in `docs/ai-usage.md`.

## Conclusion

AI output was treated as a proposal. Architecture, security, business rules, and corrections were accepted only after code review and observable test or browser evidence.
