# Generative AI evidence log

## Tool and workflow

- Codex app for macOS
- Superpowers brainstorming, writing-plans, executing-plans, test-driven-development, systematic-debugging, and verification-before-completion skills
- Human approval of the design before implementation
- Native .NET and Node execution with Docker used only for PostgreSQL and Testcontainers
- One verified local commit per planned task

## Representative implementation prompts

On 2026-11-05, the implementation planning prompt was exactly:

```text
Use Superpowers writing-plans to create an
implementation plan based on the approved design.

Break the work into small, independently testable
vertical slices, following TDD.
```

The execution constraints were then supplied exactly as:

```text
lets do native, but use [$superpowers:test-driven-development](/Users/igal/.codex/plugins/cache/openai-curated-remote/superpowers/7.0.1/skills/test-driven-development/SKILL.md) . Red-Green flow
```

```text
for each task, do a commit
```

```text
one important thing is, commit only when the task is verified with a test
```

## Representative AI-generated code

The following domain rule was generated in `src/TaskBoard.Domain/Tasks/TaskItem.cs` and committed in `1ca42e2`:

```csharp
if (dueDate != DueDate)
{
    ValidateDueDate(dueDate, utcToday);
}
```

It implements the approved rule that a task may retain an already overdue date, while a newly selected past date remains invalid. Unit tests cover both the accepted unchanged value and rejection without partial mutation.

## Evaluation and corrections

| Proposed code or assumption | How it was validated | Correction or improvement | Evidence |
| --- | --- | --- | --- |
| JWT values could be captured eagerly while composing the host | A real `WebApplicationFactory` authentication test returned 500 because test configuration is applied later | Bound bearer options after final configuration so both production startup and test overrides use the effective values | Task 5 tests, commit `3a5bbe6` |
| A fixed test clock would automatically control IdentityModel lifetime validation | Expired-token tests showed validation still consulted another clock | Added an explicit lifetime validator using the injected `TimeProvider` and preserved normal issuer, audience, signature, and skew checks | Task 7 tests, commit `114019d` |
| The generated mobile account navigation needed no extra constraint | Visual inspection at 390 px showed `Sign in` and `Create account` wrapping despite sufficient width | Added a failing computed-style regression test, then applied `white-space: nowrap` | `frontend/taskboard-web/src/app/app.spec.ts`, Task 12 |
| Angular build code 134 indicated a source failure | The same build stopped before diagnostics inside the process-restricted sandbox | Re-ran the unchanged build in the approved native environment, where it completed; no product code was changed for the environment failure | Final verification on 2026-11-06 |

## Security and edge cases

- Duplicate normalized registration: concurrent database integration coverage proves one insert and one deterministic conflict; the final HTTP demo also returned `409` for a case-and-space variant.
- Invalid JWT: protected task listing returned `401` for a malformed bearer value.
- Per-user isolation: user B received the same `404` for user A's read, replace, and delete operations as for a missing task.
- Password handling: 15 and 128 character Unicode/space boundaries are covered; passwords are not trimmed, logged, returned, or stored outside the adaptive hash.
- Invalid task fields: unit, API, and PostgreSQL tests cover blank/oversized text, invalid status, and a changed past due date. The final HTTP demo returned stable code `task.due_date.in_past`.
- SQL safety: all user values use typed Npgsql parameters. Integration coverage stores quotes and SQL-looking text literally without schema changes.
- Missing IDs: owner-scoped API tests verify stable `404` responses without revealing whether another user owns the identifier.

## Verification log

Fresh evidence recorded on 2026-11-06:

| Command or check | Result |
| --- | --- |
| `PATH="/Applications/Docker.app/Contents/Resources/bin:$PATH" bash scripts/check-prereqs.sh` | .NET 10.0.301, Node 26.13.0, Docker Compose available |
| `docker compose down -v`, `docker compose up -d`, `docker compose ps` | TaskBoard-only volume recreated; PostgreSQL 17 reported healthy |
| `dotnet test TaskBoard.slnx` | 52 unit tests and 34 integration tests passed; 0 failed, 0 skipped |
| `npm test -- --watch=false` | Final run: 9 files and 28 tests passed; 0 failed |
| Focused mobile navigation RED/GREEN | 1 failing computed-style assertion, then 3 of 3 app tests passed |
| `npm run build` | Final production bundle completed; initial 266.01 kB, estimated transfer 72.53 kB |
| Local API demo path | Ping, seeded login, `/me`, seeded list, create, read, WIP replacement, completion, delete, past date rejection, invalid JWT, duplicate registration, and three cross-user isolation checks all returned expected statuses |
| Browser inspection | Login, registration, board, edit state, desktop layout, 390 by 844 mobile layout, keyboard focus, and browser warning/error console checked; console was empty |
