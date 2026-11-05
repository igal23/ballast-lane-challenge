# TaskBoard MVP Design

## Intent

Build a small task-management application that demonstrates Clean Architecture, secure password authentication, strict per-user data isolation, direct PostgreSQL access, and evidence-driven TDD. The application serves individual registered users who need to manage only their own tasks. It must be straightforward to run and demonstrate locally.

## Goals

- Register and authenticate users, then issue short-lived JWT access tokens.
- Let an authenticated user create, list, view, replace, and delete only their own tasks.
- Keep business rules independent of ASP.NET Core, PostgreSQL, and Angular.
- Use explicit, parameterized Npgsql SQL and test it against real PostgreSQL.
- Provide a repeatable Development demo with a seeded user and sample tasks.
- Keep the implementation small enough to explain during a code review.

## Non-goals and anti-goals

- No refresh tokens, password reset, email verification, MFA, roles, shared tasks, subtasks, tags, pagination, search, audit history, or user deletion.
- No status-transition workflow: `Pending`, `Wip`, and `Completed` may change freely.
- No EF Core, Dapper, mediator library, generic repository, CQRS infrastructure, event bus, microservice split, NgRx, or SSR.
- No business rules invented merely to create layers. No client-supplied owner identity is accepted or trusted.

## Fixed constraints and recorded choices

The design retains ADR-001 through ADR-006: one controller-based ASP.NET Core API, four backend projects, Angular 22, PostgreSQL through raw Npgsql, JWT bearer authentication, controller-based MVC interpretation, and real-database integration testing. Application code must not be implemented until this design is approved.

## Approaches considered

1. **Pragmatic domain model with broad application services.** Compact, but use cases and their dependencies become less explicit.
2. **Validation concentrated in application services.** Initially simple, but domain invariants are easier to bypass and business behavior becomes less cohesive.
3. **One application use-case class per operation, selected.** Each endpoint invokes a dedicated class directly. This costs a few extra types, but makes authorization inputs, behavior, and tests explicit. There is no mediator, common handler pipeline, or generic dispatch abstraction.

## Architecture and dependency boundaries

```text
Angular SPA -> HTTP -> Api -> Application -> Domain
                         |          ^
                         v          |
                    Infrastructure -+
                    (implements Application ports)
```

- **Domain**: `TaskItem`, `TaskStatus`, domain validation errors, and task invariants. It has no project references.
- **Application**: dedicated auth/task use cases, request/result models, repository ports, password-hashing and token-issuing ports, `ICurrentDate`, and transaction/database-initialization ports where needed. It references Domain only.
- **Infrastructure**: Npgsql repositories, schema migration and Development seeding, ASP.NET password-hasher adapter, JWT issuer, and system clock. It references Application and Domain.
- **Api**: controllers, HTTP DTOs, authentication/authorization configuration, exception/result-to-HTTP mapping, dependency composition, and environment-specific startup. It references Application and Infrastructure only for composition; it contains no SQL or business rules.
- **Angular**: login/register and task screens with a small auth service, task service, interceptor, and local component state. It never decides authorization and never sends an owner ID.

Use cases are concrete classes such as `RegisterUser`, `LoginUser`, `GetCurrentUser`, `ListTasks`, `GetTask`, `CreateTask`, `ReplaceTask`, and `DeleteTask`. Controllers call them directly through constructor injection. Shared helpers are introduced only for actual duplicated policy, not to recreate a mediator framework.

## Domain model and rules

### TaskItem

- `Id`: server-generated UUID.
- `OwnerId`: authenticated user's UUID, supplied by the application layer, never an API request.
- `Title`: required after trimming, 1 to 200 characters.
- `Description`: optional; trim surrounding whitespace, store `null` when blank, maximum 2,000 characters.
- `Status`: exactly `Pending`, `Wip`, or `Completed`; all transitions are allowed.
- `DueDate`: optional `DateOnly`.

Creation rejects a supplied due date earlier than the current UTC date. Replacement rejects a newly assigned or changed due date earlier than the current UTC date. An unchanged date may naturally become overdue and does not prevent changing status or other fields. This preserves usable overdue tasks without allowing users to choose a new past deadline. The UTC date comes from `ICurrentDate`, so tests do not depend on the machine clock. Converting or displaying dates for a user's timezone is deferred until the UI's timezone behavior is designed.

The domain exposes controlled creation and replacement behavior rather than public setters. Validation returns stable error codes and field names; HTTP wording remains an API concern.

### User identity

Authentication uses a persisted user record with server-generated UUID, normalized email, and password hash. Email is trimmed, normalized to lowercase invariant form, syntactically validated, limited to 254 characters, and uniquely constrained by its normalized value.

Because this MVP has no MFA, passwords must be 15 to 128 characters. All characters, including spaces and Unicode, are accepted; passwords are not trimmed, silently truncated, or subjected to arbitrary composition rules. Hashing uses the established ASP.NET Core password hasher. Passwords and hashes never appear in responses or logs. A breached-password service and password-strength meter are deferred because they add external/runtime scope to the local MVP.

## HTTP API

All errors use one predictable Problem Details shape with a stable application error code and optional field errors. Malformed JSON, malformed UUIDs, and validation failures return `400`. Protected endpoints return `401` for a missing, invalid, or expired token.

| Method and route | Access | Success | Important failures |
|---|---|---:|---|
| `POST /api/auth/register` | Anonymous | `201` | `400` invalid input; `409` generic registration conflict |
| `POST /api/auth/login` | Anonymous | `200` token response | `400` malformed input; `401` generic invalid credentials |
| `GET /api/auth/me` | Authenticated | `200` current user | `401` invalid token |
| `GET /api/public/ping` | Anonymous | `200` | none |
| `GET /api/tasks` | Authenticated | `200` array, including empty | `401` |
| `GET /api/tasks/{id}` | Authenticated | `200` | `400` malformed ID; `404` absent or foreign-owned |
| `POST /api/tasks` | Authenticated | `201` plus `Location` | `400`; `401` |
| `PUT /api/tasks/{id}` | Authenticated | `200` replaced task | `400`; `404` absent or foreign-owned |
| `DELETE /api/tasks/{id}` | Authenticated | `204` | `404` absent or foreign-owned |

Task request DTOs contain only title, description, status, and due date. `PUT` is a full replacement of those editable fields. IDs and owner IDs come from the route, server, and validated JWT. Lists use a deterministic order: incomplete before completed, then due date with nulls last, then UUID as a stable tie-breaker. Pagination is unnecessary for seeded MVP-sized data.

JWTs are signed from configuration and validate signature, issuer, audience, and lifetime with a small clock-skew allowance. The subject claim contains the user UUID. Tokens expire after 60 minutes; there is no refresh flow. Secrets and production connection strings are configuration/environment values, not committed source. Swagger/OpenAPI is Development-only, and authorization is enforced by middleware plus owner-scoped persistence.

## Per-user isolation

The API derives the current user UUID only from the validated JWT subject. Each task repository method that reads or mutates an existing row requires both task ID and owner ID. SQL includes both in the predicate, for example `WHERE id = @id AND owner_id = @owner_id`; it never loads by ID and authorizes afterward. Foreign-owned and nonexistent IDs therefore produce the same `404` result and do not reveal existence. Inserts always use the authenticated owner ID.

## PostgreSQL persistence

Infrastructure owns a singleton `NpgsqlDataSource`. Repositories create commands explicitly, bind every user-influenced value as a parameter with an appropriate PostgreSQL type, and map columns explicitly. No dynamic SQL contains request values.

Initial tables:

- `users(id uuid primary key, email varchar(254), normalized_email varchar(254) unique, password_hash text)`
- `tasks(id uuid primary key, owner_id uuid references users(id), title varchar(200), description varchar(2000) null, status varchar(16), due_date date null)`
- `schema_versions(version integer primary key, applied_at timestamptz)`

Database constraints mirror safe invariants such as lengths, nonblank title, and allowed status values. Date validity stays in the domain because its truth changes with time. A small startup migrator applies ordered, versioned SQL scripts transactionally and records them in `schema_versions`; it is not a general migration framework. Startup fails clearly if migration fails.

The Development-only seeder runs after migrations, is idempotent, hashes the documented demo password at runtime, and creates one demo user plus representative pending, work-in-progress, and completed tasks. It cannot run outside Development. Exact credentials and commands will be added to the README during implementation.

Registration relies on the database unique constraint as the final concurrency-safe duplicate-email guard. Infrastructure maps PostgreSQL unique-violation metadata to a stable duplicate result rather than matching message text. Multi-statement operations use an explicit Npgsql transaction only where atomicity requires it.

## TDD and test strategy

Implementation proceeds in small RED, GREEN, REFACTOR slices, retaining meaningful evidence in commits and `docs/ai-usage.md` without fabricating failed runs.

- **Domain unit tests**: trimming and length boundaries, all status values and free transitions, optional due date, yesterday/today/future rules, unchanged overdue date, and replacement atomicity.
- **Application unit tests**: one suite per use case using focused fakes/mocks; owner ID propagation, duplicate email, invalid password, not-found mapping, token creation, empty list, and repository non-calls on invalid input.
- **API/controller tests**: routing, DTO mapping, Problem Details, status codes, `Location`, anonymous/public behavior, and authenticated subject extraction.
- **Repository integration tests against real PostgreSQL**: explicit mappings, CRUD persistence, deletion, unique normalized email under duplicate attempts, constraints, transaction behavior, and owner-scoped queries/mutations.
- **End-to-end API integration tests against real PostgreSQL**: register/login/me, JWT rejection, complete task CRUD, validation, persistence across requests, and two-user cross-access returning `404` for get/replace/delete.
- **Angular tests and build**: service/interceptor behavior and key component states, followed by a production build and a manual responsive demo check with a clean browser console.

Database tests use isolated schemas or databases per test collection and deterministic cleanup. Testcontainers supplies real PostgreSQL when Docker is available. Tests that cannot run because Docker is unavailable are reported as blocked, never silently treated as passing.

## Security and edge cases

- Generic login failure for unknown email and incorrect password; duplicate registration has one deterministic generic conflict response.
- Parameterized SQL everywhere, bounded inputs, no password/token logging, and no sensitive Problem Details.
- Invalid status casing/value, blank or oversized title, oversized description, invalid email/password, and invalid dates fail before persistence.
- Unknown and foreign-owned task IDs are indistinguishable; delete is persistent; empty lists are successful.
- Database uniqueness handles concurrent registrations. Task updates use last-write-wins because concurrency tokens are outside MVP scope.
- CORS is restricted to the configured Angular development origin. HTTPS redirection and secure production configuration remain enabled; local HTTP exceptions, if required, are Development-only.

## Local demo readiness

The final README will give exact prerequisite, database, API, frontend, test, reset, and demo commands. Docker Compose provides PostgreSQL on the recorded local port. The demo path is: public ping, login as the seeded user, show seeded tasks, create and edit a task, complete it, delete it, then demonstrate an invalid date and a cross-user `404`. Startup and health failures must be visible and actionable rather than masked.

## Decisions left to the implementation plan

The plan may choose file/folder names, exact result-type mechanics, test fixture organization, SQL embedding mechanics, and Angular component boundaries, provided they preserve these dependency rules and avoid new frameworks. Any change to the API contract, security policy, domain rules, or recorded stack requires design approval.
