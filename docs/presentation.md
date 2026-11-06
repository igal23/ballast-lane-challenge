# English technical panel presentation outline

Use as a speaking guide, not a full slide deck. Target a concise introduction and demo, leaving substantial time for live code review and questions.

## 1. User story and MVP scope

- TaskBoard gives one authenticated user a focused personal task list.
- The MVP supports registration, login, owner-scoped list/create/read/replace/delete, three statuses, and an optional date-only due date.
- Intentionally omitted: sharing, roles, pagination, refresh tokens, notifications, and client timezone display rules.

## 2. Architecture and design decisions

- Dependency direction: `Api -> Application -> Domain`; `Infrastructure -> Application`; Domain has no outward project dependency.
- One controller-based ASP.NET Core host keeps deployment small. Controllers invoke explicit use-case classes directly without a mediator.
- PostgreSQL is accessed through `NpgsqlDataSource`, typed parameters, explicit SQL, and explicit row mapping because the assignment prohibits EF and Dapper.
- Domain owns task invariants. Application owns workflows and ports. Infrastructure owns hashing, JWT issuance, UTC time, migrations, seeding, and SQL adapters. API owns authentication middleware and HTTP mapping.

## 3. Demonstration

1. Run PostgreSQL, API, and Angular with the README commands.
2. Login as `demo@taskboard.local` and show the three seeded statuses.
3. Create a task with an optional due date, edit its status, retain an unchanged overdue date if demonstrating the rule, then use inline delete confirmation.
4. Show that ping is public while tasks require a bearer token.
5. Show `TaskOwnershipApiTests` or the final two-user HTTP evidence: foreign read, replace, and delete all return `404`.

## 4. Testing and TDD

- Show the mobile navigation regression: computed style was `normal`, the test failed, CSS changed to `nowrap`, then the focused test passed.
- Unit tests cover domain behavior, application workflows, and controller mapping.
- Integration tests use real PostgreSQL through Testcontainers and exercise the hosted API.
- Final evidence: 52 unit, 34 integration, and 28 Angular tests after the responsive regression, plus a successful production build.

## 5. Critical GenAI usage

- Show the exact TDD prompt and commit-per-task constraint from `docs/ai-usage.md`.
- Show the generated unchanged-overdue-date rule in `TaskItem.Replace`.
- Explain the JWT configuration timing correction and the mobile navigation correction, including the tests that exposed them.

## 6. Trade-offs and follow-ups

- Session storage reduces token persistence but does not provide refresh or revocation. A production follow-up would add a short-lived access token, refresh rotation, and stronger browser security policy.
- List queries are intentionally unpaginated for the small MVP. Add cursor pagination once task volume requires it.
- The migration runner is intentionally small and versioned. A larger system could adopt a dedicated migration tool without changing repository boundaries.
- Dates remain server-side UTC date values. A later UI decision can add per-user timezone conversion and display without changing the current persistence contract.
- Be ready to explain typed SQL parameters, owner predicates, generic authentication failures, password hashing, `404` isolation, and dependency injection.
