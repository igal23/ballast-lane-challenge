# TaskBoard MVP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver a locally runnable TaskBoard MVP with secure JWT authentication, strictly owner-scoped task CRUD, direct Npgsql persistence, an Angular client, and repeatable automated tests.

**Architecture:** Build one ASP.NET Core controller host with Domain, Application, Infrastructure, and Api projects. Each API operation calls its own Application use-case class directly; Infrastructure implements ports with parameterized Npgsql SQL. Add the Angular client only after the HTTP contract is covered against real PostgreSQL.

**Tech Stack:** .NET 10, C# 14, ASP.NET Core controllers, Npgsql, PostgreSQL 17, JWT bearer authentication, ASP.NET Core password hashing, xUnit, Moq, Testcontainers for PostgreSQL, Angular 22, TypeScript, SCSS.

**Spec:** `docs/superpowers/specs/2026-11-05-taskboard-mvp-design.md`

## Global Constraints

- Domain has no project references; Application references Domain; Infrastructure references Application; Api composes Application and Infrastructure.
- Use one explicit Application use-case class per operation, with direct controller injection and no mediator or generic dispatch abstraction.
- Do not use Entity Framework, Dapper, MediatR, a generic repository, event bus, or additional service host.
- Use `NpgsqlDataSource`, `NpgsqlCommand`, explicit column mapping, and parameters for every user-influenced SQL value.
- Task statuses are exactly `Pending`, `Wip`, and `Completed`, with unrestricted transitions.
- Due date is optional; a new or changed date must be on or after `ICurrentDate.UtcToday`; an unchanged overdue date remains valid.
- Passwords are 15 to 128 characters, are never trimmed or truncated, and have no composition rule. Email is trimmed, syntax-checked, lower-invariant normalized, and capped at 254 characters.
- Resolve owner identity only from the validated JWT `sub` claim and include `owner_id` in every existing-task SQL predicate. Foreign and missing task IDs both return `404`.
- Use stable Problem Details error codes. Do not log passwords, hashes, access tokens, or sensitive database details.
- Follow RED, GREEN, REFACTOR for business behavior. Run the narrow test first, then the affected suite, and make a small descriptive commit for each task.
- Never fabricate test output, AI evidence, or demo results. Record unavailable Docker or tool dependencies as blocked.

## Review Focus

- A password containing spaces or Unicode at the 15 and 128 character boundaries is accepted without trimming, while 14 and 129 characters are rejected. Task 4 pins this behavior.
- Two registrations racing on the same normalized email produce one user and one deterministic `409`, without relying on PostgreSQL message text. Task 4 pins this behavior.
- Due-date validation uses the UTC date at the day boundary, and an unchanged overdue date does not block replacement. Tasks 2 and 7 pin this behavior.
- A valid token for user B cannot reveal, replace, or delete user A's task; all three operations return the same `404` as a missing ID. Task 7 pins this behavior.
- Quotes and SQL metacharacters in valid title/description text are stored literally and do not alter the schema. Task 6 pins this behavior.

---

## Planned file structure

```text
src/TaskBoard.Domain/
  Tasks/TaskItem.cs, TaskStatus.cs
  Validation/DomainValidationException.cs
  Users/UserAccount.cs
src/TaskBoard.Application/
  Common/{AppError,Result,Outcome}.cs
  Abstractions/{ICurrentDate,IPasswordHasher,IAccessTokenIssuer,IUserRepository,ITaskRepository}.cs
  Auth/{RegisterUser,LoginUser,GetCurrentUser,AuthModels}.cs
  Tasks/{CreateTask,ListTasks,GetTask,ReplaceTask,DeleteTask,TaskModels}.cs
src/TaskBoard.Infrastructure/
  Auth/{AspNetPasswordHasher,JwtAccessTokenIssuer}.cs
  Persistence/{DatabaseMigrator,DevelopmentSeeder,NpgsqlUserRepository,NpgsqlTaskRepository}.cs
  Persistence/Migrations/001_Initial.sql
  Time/UtcCurrentDate.cs
  DependencyInjection.cs
src/TaskBoard.Api/
  Controllers/{Auth,Public,Tasks}Controller.cs
  Contracts/AuthContracts.cs, TaskContracts.cs
  Http/{ClaimsPrincipalExtensions,ResultMapping}.cs
  Program.cs, appsettings.json
tests/TaskBoard.UnitTests/{Domain,Application,Api}/...
tests/TaskBoard.IntegrationTests/{Database,Api,Support}/...
frontend/taskboard-web/src/app/
  core/auth/..., core/tasks/..., features/auth/..., features/tasks/...
```

Keep one public type per focused file when it owns behavior. The grouped contract/model files contain only small immutable records and enums that change together.

### Task 1: Bootstrap the solution and prove the public API path

**Files:**
- Generate: `TaskBoard.slnx`, `src/**`, `tests/**`, `frontend/taskboard-web/**` via `bash scripts/bootstrap.sh`
- Modify: generated project files and `src/TaskBoard.Api/Program.cs`
- Create: `src/TaskBoard.Api/Controllers/PublicController.cs`
- Create: `tests/TaskBoard.IntegrationTests/Api/PublicApiTests.cs`
- Delete: generated `Class1.cs`, weather forecast controller/model, and placeholder tests

**Interfaces:**
- Produces: `GET /api/public/ping` returning `200` with `{ "status": "ok" }`; a buildable four-project backend; Angular 22 shell.

- [ ] **Step 1: Run the approved one-time scaffold and add only required test/runtime packages**

Run `bash scripts/bootstrap.sh`, then add `Npgsql` and `Microsoft.Extensions.Identity.Core` to Infrastructure; `Microsoft.AspNetCore.Authentication.JwtBearer` to Api; `Moq` and an Api project reference to UnitTests; `Microsoft.AspNetCore.Mvc.Testing`, `Testcontainers.PostgreSql`, and `Npgsql` to IntegrationTests. Remove generated sample files.

- [ ] **Step 2: Write the failing public endpoint integration test**

```csharp
[Fact]
public async Task Ping_ReturnsOkWithoutAuthentication()
{
    var response = await _client.GetAsync("/api/public/ping");
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Equal("ok", (await response.Content.ReadFromJsonAsync<PingResponse>())!.Status);
}
```

- [ ] **Step 3: Run the narrow test and confirm RED**

Run: `dotnet test tests/TaskBoard.IntegrationTests --filter Ping_ReturnsOkWithoutAuthentication`
Expected: FAIL because the route does not exist.

- [ ] **Step 4: Add the minimal controller and test host support**

Implement `PublicController.Ping(): ActionResult<PingResponse>` at `/api/public/ping`; expose `public partial class Program` for `WebApplicationFactory<Program>`. Keep Swagger/OpenAPI Development-only.

- [ ] **Step 5: Verify GREEN and commit**

Run `dotnet test TaskBoard.slnx` and `npm test -- --watch=false` from `frontend/taskboard-web`; expect all scaffold tests to pass. Commit with `chore: bootstrap TaskBoard solution`.

### Task 2: Define task domain behavior

**Files:**
- Create: `src/TaskBoard.Domain/Tasks/TaskStatus.cs`
- Create: `src/TaskBoard.Domain/Tasks/TaskItem.cs`
- Create: `src/TaskBoard.Domain/Validation/DomainValidationException.cs`
- Create: `tests/TaskBoard.UnitTests/Domain/TaskItemTests.cs`

**Interfaces:**
- Produces: `TaskItem.Create(Guid id, Guid ownerId, string title, string? description, TaskStatus status, DateOnly? dueDate, DateOnly utcToday)`; `TaskItem.Rehydrate(...)`; `void Replace(string title, string? description, TaskStatus status, DateOnly? dueDate, DateOnly utcToday)`; read-only properties `Id`, `OwnerId`, `Title`, `Description`, `Status`, `DueDate`.
- Produces: `DomainValidationException.Code` and `.Field` with codes `task.title.required`, `task.title.too_long`, `task.description.too_long`, and `task.due_date.in_past`.

- [ ] **Step 1: Write failing domain tests for creation boundaries**

```csharp
[Theory] // blank title; 201-char title; 2001-char description; yesterday
public void Create_InvalidInput_ThrowsStableFieldError(...) { /* assert Code and Field */ }

[Fact]
public void Create_TrimsTextAndAcceptsOptionalOrTodayDueDate() { /* assert normalized values */ }
```

- [ ] **Step 2: Run the domain tests and confirm RED**

Run: `dotnet test tests/TaskBoard.UnitTests --filter FullyQualifiedName~TaskItemTests`
Expected: FAIL to compile because the domain types do not exist.

- [ ] **Step 3: Implement the minimal entity and enum**

Validate all replacement inputs before assigning any property. Trim title and description; convert blank description to `null`. `Rehydrate` trusts already constrained database values and does not compare due date to today.

- [ ] **Step 4: Add failing replacement tests, then implement overdue behavior**

```csharp
[Fact] public void Replace_AllowsAnyStatusTransition() { /* Pending -> Completed -> Wip */ }
[Fact] public void Replace_AllowsUnchangedOverdueDate() { /* same past date succeeds */ }
[Fact] public void Replace_RejectsChangedPastDateWithoutPartialMutation() { /* all old values remain */ }
```

Run the filter before and after implementation; expect RED on the new cases, then GREEN for all `TaskItemTests`.

- [ ] **Step 5: Run the unit suite and commit**

Run `dotnet test tests/TaskBoard.UnitTests`; expect PASS. Commit with `feat: define task domain rules`.

### Task 3: Add versioned PostgreSQL schema and real-database test support

**Files:**
- Create: `src/TaskBoard.Infrastructure/Persistence/Migrations/001_Initial.sql`
- Create: `src/TaskBoard.Infrastructure/Persistence/DatabaseMigrator.cs`
- Create: `src/TaskBoard.Infrastructure/DependencyInjection.cs`
- Create: `tests/TaskBoard.IntegrationTests/Support/PostgresFixture.cs`
- Create: `tests/TaskBoard.IntegrationTests/Database/DatabaseMigratorTests.cs`
- Modify: `src/TaskBoard.Infrastructure/TaskBoard.Infrastructure.csproj`
- Modify: `src/TaskBoard.Api/Program.cs`

**Interfaces:**
- Produces: `DatabaseMigrator(NpgsqlDataSource)` and `Task MigrateAsync(CancellationToken)`.
- Produces: singleton `NpgsqlDataSource` registered from `ConnectionStrings:TaskBoard` and a reusable PostgreSQL 17 Testcontainers fixture exposing `ConnectionString`.

- [ ] **Step 1: Write the failing migration integration test**

```csharp
[Fact]
public async Task MigrateAsync_CreatesSchemaOnceAndIsIdempotent()
{
    await migrator.MigrateAsync(default);
    await migrator.MigrateAsync(default);
    Assert.Equal(new[] { "schema_versions", "tasks", "users" }, await ReadApplicationTables());
    Assert.Equal(1, await CountAppliedVersions());
}
```

- [ ] **Step 2: Run the database test and confirm RED**

Run: `dotnet test tests/TaskBoard.IntegrationTests --filter MigrateAsync_CreatesSchemaOnceAndIsIdempotent`
Expected: FAIL because the migrator/schema do not exist. If Docker is unavailable, stop and report this slice as blocked.

- [ ] **Step 3: Implement the schema and transactional migrator**

Create the exact tables and constraints from the design. Name the unique constraint `uq_users_normalized_email`, the status check `ck_tasks_status`, and add `idx_tasks_owner_sort` for owner-scoped ordering. Embed ordered SQL files in the Infrastructure assembly; apply each in a transaction and record its integer version.

- [ ] **Step 4: Verify schema constraints with real PostgreSQL**

Add tests that direct SQL rejects blank titles, oversized fields, invalid status, and orphan owners. Run all `DatabaseMigratorTests`; expect PASS.

- [ ] **Step 5: Run backend tests and commit**

Run `dotnet test TaskBoard.slnx`; expect PASS. Commit with `feat: add PostgreSQL schema migrations`.

### Task 4: Deliver user registration end to end

**Files:**
- Create: `src/TaskBoard.Domain/Users/UserAccount.cs`
- Create: `src/TaskBoard.Application/Common/{Outcome,AppError,Result}.cs`
- Create: `src/TaskBoard.Application/Abstractions/{IUserRepository,IPasswordHasher}.cs`
- Create: `src/TaskBoard.Application/Auth/{AuthModels,RegisterUser}.cs`
- Create: `src/TaskBoard.Infrastructure/Auth/AspNetPasswordHasher.cs`
- Create: `src/TaskBoard.Infrastructure/Persistence/NpgsqlUserRepository.cs`
- Create: `src/TaskBoard.Api/Contracts/AuthContracts.cs`
- Create: `src/TaskBoard.Api/Http/ResultMapping.cs`
- Create: `src/TaskBoard.Api/Controllers/AuthController.cs`
- Create: `tests/TaskBoard.UnitTests/Application/RegisterUserTests.cs`
- Create: `tests/TaskBoard.UnitTests/Api/AuthControllerTests.cs`
- Create: `tests/TaskBoard.IntegrationTests/Database/NpgsqlUserRepositoryTests.cs`
- Create: `tests/TaskBoard.IntegrationTests/Api/RegistrationApiTests.cs`
- Create: `tests/TaskBoard.IntegrationTests/Support/TaskBoardApiFactory.cs`

**Interfaces:**
- Produces: `IUserRepository.GetByNormalizedEmailAsync`, `GetByIdAsync`, and `AddAsync`; `AddAsync` returns `false` only for PostgreSQL constraint `uq_users_normalized_email`.
- Produces: `IPasswordHasher.Hash(string)` and `Verify(string? hash, string suppliedPassword)`; `Verify(null, ...)` performs a real dummy-hash verification and returns false.
- Produces: `RegisterUser.ExecuteAsync(RegisterUserCommand, CancellationToken): Task<Result<UserDto>>`.

- [ ] **Step 1: Write failing use-case tests for validation and normalization**

```csharp
[Theory] // malformed email, 255-char email, 14-char password, 129-char password
public async Task Execute_InvalidRegistration_ReturnsValidationWithoutRepositoryCall(...) { }

[Fact]
public async Task Execute_ValidRegistration_NormalizesEmailAndStoresOnlyHash() { }
```

Include 15- and 128-character passwords containing spaces and Unicode; assert they reach `Hash` unchanged. Run the RegisterUser filter and confirm RED.

- [ ] **Step 2: Implement the minimal domain record, shared result, ports, and use case**

Use `Outcome.Success`, `ValidationError`, `Conflict`, `Unauthorized`, and `NotFound`. Validate email syntax with `EmailAddressAttribute.IsValid`. Return stable codes `auth.email.invalid`, `auth.email.too_long`, `auth.password.length`, and `auth.registration.conflict`.

- [ ] **Step 3: Write failing repository tests for duplicate concurrency, then implement Npgsql registration**

Start two `AddAsync` calls for the same normalized email; assert exactly one succeeds, one returns false, and one row exists. Map by SQLSTATE `23505` plus `ConstraintName`, never message text. Run the repository filter RED then GREEN.

- [ ] **Step 4: Write failing controller and API tests, then expose registration**

At controller level, assert success maps to `201`, validation to stable `400` Problem Details, and conflict to generic `409`. At API level, assert `POST /api/auth/register` returns public user data and a second normalized duplicate returns `409` without a password/hash. Wire the real PostgreSQL factory, migrations, repository, and hasher.

- [ ] **Step 5: Run affected suites and commit**

Run `dotnet test TaskBoard.slnx`; expect PASS. Commit with `feat: add user registration`.

### Task 5: Deliver login, JWT authentication, and current-user lookup

**Files:**
- Create: `src/TaskBoard.Application/Abstractions/IAccessTokenIssuer.cs`
- Create: `src/TaskBoard.Application/Auth/{LoginUser,GetCurrentUser}.cs`
- Create: `src/TaskBoard.Infrastructure/Auth/JwtAccessTokenIssuer.cs`
- Create: `src/TaskBoard.Api/Http/ClaimsPrincipalExtensions.cs`
- Modify: `src/TaskBoard.Api/Controllers/AuthController.cs`
- Modify: `src/TaskBoard.Api/Program.cs`, `src/TaskBoard.Api/appsettings.json`
- Create: `tests/TaskBoard.UnitTests/Application/{LoginUser,GetCurrentUser}Tests.cs`
- Modify: `tests/TaskBoard.UnitTests/Api/AuthControllerTests.cs`
- Create: `tests/TaskBoard.IntegrationTests/Api/AuthenticationApiTests.cs`

**Interfaces:**
- Produces: `AccessTokenDto(string AccessToken, DateTimeOffset ExpiresAt)` and `IAccessTokenIssuer.Issue(UserAccount): AccessTokenDto`.
- Produces: `LoginUser.ExecuteAsync(LoginUserCommand, CancellationToken): Task<Result<AccessTokenDto>>` and `GetCurrentUser.ExecuteAsync(Guid, CancellationToken): Task<Result<UserDto>>`.
- Produces: authenticated `POST /api/auth/login` and `GET /api/auth/me`; JWT `sub` is the UUID, lifetime is 60 minutes, and clock skew is 30 seconds.

- [ ] **Step 1: Write failing login unit tests**

Assert valid credentials issue a token; wrong password and missing email both call `Verify` once and return the same `auth.invalid_credentials`; no token is issued on failure. Confirm RED, then implement the two use cases.

- [ ] **Step 2: Write failing JWT issuer tests**

Assert issuer, audience, `sub`, issued/expiry times, signature validation, and exactly 60-minute lifetime using a controlled `TimeProvider`. Confirm RED, then implement `JwtAccessTokenIssuer` with configuration validation at startup.

- [ ] **Step 3: Write failing controller and API authentication tests**

At controller level, assert successful login maps to `200`, invalid credentials to generic `401`, and `/me` uses only the authenticated subject ID. At API level, assert `/me` returns the current user's ID/email; missing, malformed, wrong-signature, and expired tokens return `401`; invalid credentials return the same generic `401` body for missing user and wrong password.

- [ ] **Step 4: Configure bearer authentication and expose endpoints**

Validate issuer, audience, lifetime, and signature. Read the signing key only from `Jwt:SigningKey` configuration. Add `[Authorize]` to `/me` and parse only a validated `sub` claim.

- [ ] **Step 5: Run backend tests and commit**

Run `dotnet test TaskBoard.slnx`; expect PASS. Commit with `feat: add JWT authentication`.

### Task 6: Deliver create and list task slices

**Files:**
- Create: `src/TaskBoard.Application/Abstractions/{ICurrentDate,ITaskRepository}.cs`
- Create: `src/TaskBoard.Application/Tasks/{TaskModels,CreateTask,ListTasks}.cs`
- Create: `src/TaskBoard.Infrastructure/Time/UtcCurrentDate.cs`
- Create: `src/TaskBoard.Infrastructure/Persistence/NpgsqlTaskRepository.cs`
- Create: `src/TaskBoard.Api/Contracts/TaskContracts.cs`
- Create: `src/TaskBoard.Api/Controllers/TasksController.cs`
- Create: `tests/TaskBoard.UnitTests/Application/{CreateTask,ListTasks}Tests.cs`
- Create: `tests/TaskBoard.UnitTests/Api/TasksControllerTests.cs`
- Create: `tests/TaskBoard.IntegrationTests/Database/NpgsqlTaskRepositoryTests.cs`
- Create: `tests/TaskBoard.IntegrationTests/Api/CreateAndListTasksApiTests.cs`

**Interfaces:**
- Produces: `ICurrentDate.UtcToday` and `ITaskRepository.AddAsync`, `ListAsync(Guid ownerId, CancellationToken)`.
- Produces: `CreateTask.ExecuteAsync(Guid ownerId, TaskWriteCommand, CancellationToken)` and `ListTasks.ExecuteAsync(Guid ownerId, CancellationToken)` returning `TaskDto` records without owner IDs.

- [ ] **Step 1: Write failing use-case tests**

Assert the authenticated owner is assigned regardless of request content; exact status parsing is case-sensitive; invalid title/status/date never calls the repository; list passes the owner ID and preserves repository order. Confirm RED, then implement use cases using the domain entity.

- [ ] **Step 2: Write failing Npgsql repository tests**

Assert insert round-trips every field; list returns only the requested owner ordered incomplete first, due date nulls last, then UUID; an empty owner list is empty. Include a valid title/description containing `'`, `;`, and SQL-looking text; assert it round-trips and `tasks` still exists.

- [ ] **Step 3: Implement parameterized insert/list SQL**

Bind UUID, varchar/text, date, and status values explicitly. Map columns by name/type and never interpolate values or sort input.

- [ ] **Step 4: Write failing controller and API tests, then expose endpoints**

At controller level, assert create maps success to `201` with `Location`, list maps an empty result to `200`, and both forward only the subject owner ID. At API level, assert normalized task bodies contain no owner ID; anonymous requests return `401`; invalid fields return stable `400` Problem Details.

- [ ] **Step 5: Run backend tests and commit**

Run `dotnet test TaskBoard.slnx`; expect PASS. Commit with `feat: add task creation and listing`.

### Task 7: Complete task read, replacement, deletion, and isolation

**Files:**
- Create: `src/TaskBoard.Application/Tasks/{GetTask,ReplaceTask,DeleteTask}.cs`
- Modify: `src/TaskBoard.Application/Abstractions/ITaskRepository.cs`
- Modify: `src/TaskBoard.Infrastructure/Persistence/NpgsqlTaskRepository.cs`
- Modify: `src/TaskBoard.Api/Controllers/TasksController.cs`
- Create: `tests/TaskBoard.UnitTests/Application/{GetTask,ReplaceTask,DeleteTask}Tests.cs`
- Modify: `tests/TaskBoard.UnitTests/Api/TasksControllerTests.cs`
- Create: `tests/TaskBoard.IntegrationTests/Api/TaskOwnershipApiTests.cs`
- Modify: `tests/TaskBoard.IntegrationTests/Database/NpgsqlTaskRepositoryTests.cs`

**Interfaces:**
- Produces: `ITaskRepository.GetAsync(Guid id, Guid ownerId, CancellationToken)`, `ReplaceAsync(TaskItem, CancellationToken)`, and `DeleteAsync(Guid id, Guid ownerId, CancellationToken)`.
- Produces: corresponding use cases and `GET`, `PUT`, `DELETE /api/tasks/{id}` endpoints.

- [ ] **Step 1: Write failing use-case tests**

Assert every call passes both IDs; missing repository results map to `NotFound`; replace calls domain behavior before persistence; delete returns success only when one owner-scoped row is deleted. Confirm RED, then add the use cases.

- [ ] **Step 2: Write failing repository isolation tests**

For two owners, assert get/replace/delete SQL cannot affect the other owner's task. Assert replacement is last-write-wins and deletion remains absent on a later read. Confirm RED, then implement predicates containing both `id = @id` and `owner_id = @owner_id`.

- [ ] **Step 3: Write failing controller mapping and two-user API tests**

At controller level, assert missing get/replace/delete results map to identical `404`, successful delete maps to `204`, and route plus subject IDs are forwarded unchanged. At API level, assert user A can read and replace their task; user B receives the same `404` Problem Details as a random missing UUID for GET, PUT, and DELETE; malformed UUID returns `400`; successful delete persists.

- [ ] **Step 4: Pin UTC and overdue replacement behavior through the API**

Insert an overdue task directly, then assert PUT with the unchanged due date can change status, while PUT with a different past date returns `400` and leaves all stored values unchanged. Use a fixed UTC date in the API factory.

- [ ] **Step 5: Run backend tests and commit**

Run `dotnet test TaskBoard.slnx`; expect PASS. Commit with `feat: complete owner-scoped task CRUD`.

### Task 8: Add startup migration, Development seed, and secure local API configuration

**Files:**
- Create: `src/TaskBoard.Infrastructure/Persistence/DevelopmentSeeder.cs`
- Modify: `src/TaskBoard.Infrastructure/DependencyInjection.cs`
- Modify: `src/TaskBoard.Api/Program.cs`, `src/TaskBoard.Api/appsettings.json`
- Create: `tests/TaskBoard.IntegrationTests/Database/DevelopmentSeederTests.cs`
- Create: `tests/TaskBoard.IntegrationTests/Api/StartupConfigurationTests.cs`
- Modify: `README.md`

**Interfaces:**
- Produces: `DevelopmentSeeder.SeedAsync(CancellationToken)` and startup sequence `MigrateAsync`, then seed only when `IHostEnvironment.IsDevelopment()`.
- Produces: demo user `demo@taskboard.local` with password `TaskBoard demo passphrase 2026!` and three idempotent representative tasks.

- [ ] **Step 1: Write failing seed tests**

Assert two seed calls create exactly one demo user and three tasks, store a verifiable hash rather than plaintext, and produce `Pending`, `Wip`, and `Completed`. Assert a Production host never invokes the seeder.

- [ ] **Step 2: Implement migration/seed startup and configuration guards**

Fail startup clearly when the connection string, JWT issuer/audience, signing key, or allowed origins are missing. Restrict CORS to configured origins. Keep Swagger Development-only and enable HTTPS redirection outside local test hosting.

- [ ] **Step 3: Add startup integration tests**

Assert a fresh database starts, migrates, and serves ping; a second startup is idempotent; Production omits seed data; disallowed CORS origins receive no allow-origin header.

- [ ] **Step 4: Document exact backend run and smoke commands**

Document `docker compose up -d`, environment variables for connection/JWT without committing secrets, `dotnet run`, demo credentials, curl registration/login/task examples, tests, and project-scoped database reset.

- [ ] **Step 5: Verify and commit**

Run `dotnet test TaskBoard.slnx` and a Development startup against Docker PostgreSQL; expect PASS/healthy startup. Commit with `feat: add development database bootstrap`.

### Task 9: Deliver Angular registration and login

**Files:**
- Create: `frontend/taskboard-web/src/app/core/auth/{auth.models,auth.service,auth.interceptor,auth.guard}.ts`
- Create: `frontend/taskboard-web/src/app/features/auth/{login,register}/*`
- Modify: `frontend/taskboard-web/src/app/{app.config,app.routes,app.component}.*`
- Create/modify: colocated `*.spec.ts` files and global `styles.scss`

**Interfaces:**
- Produces: `AuthService.register`, `login`, `logout`, `currentUser`, `isAuthenticated`; bearer interceptor using `sessionStorage`; protected `/tasks`; public `/login` and `/register`.

- [ ] **Step 1: Write failing service/interceptor/guard tests**

Assert register/login call exact API routes; token is stored only after success; logout clears it; interceptor adds bearer only for API requests; guard redirects anonymous users to `/login`; passwords never enter logs or persistent local storage.

- [ ] **Step 2: Implement the minimal auth core**

Use Angular signals for small in-memory auth state and `sessionStorage` for the access token. Decode no token claims for authorization; call `/api/auth/me` to restore user state.

- [ ] **Step 3: Write failing component tests**

Assert accessible labels, password length hints, disabled submit while invalid/in-flight, generic invalid-credential display, registration conflict display, and navigation to `/tasks` after success.

- [ ] **Step 4: Implement responsive login/register screens**

Use reactive forms and semantic HTML. Do not duplicate backend-only email or authorization decisions beyond immediate form guidance.

- [ ] **Step 5: Verify and commit**

Run `npm test -- --watch=false` and `npm run build`; expect PASS. Commit with `feat: add Angular authentication`.

### Task 10: Deliver Angular task list and creation

**Files:**
- Create: `frontend/taskboard-web/src/app/core/tasks/{task.models,task.service}.ts`
- Create: `frontend/taskboard-web/src/app/features/tasks/task-board/*`
- Create: `frontend/taskboard-web/src/app/features/tasks/task-form/*`
- Modify: routes and colocated specs

**Interfaces:**
- Produces: `TaskService.list()` and `create(TaskWriteRequest)`; `TaskBoardComponent` loads/refreshes; reusable `TaskFormComponent` emits valid task input without owner ID.

- [ ] **Step 1: Write failing service tests**

Assert `GET /api/tasks` and `POST /api/tasks` request/response mapping, exact status strings, date-only `YYYY-MM-DD`, and absence of any owner ID in request bodies.

- [ ] **Step 2: Implement the service and models**

Define only fields from the approved contract. Keep timezone conversion out of this MVP; render date-only values without constructing a local JavaScript `Date`.

- [ ] **Step 3: Write failing list/create component tests**

Assert loading, empty, populated, and API error states; valid create refreshes/inserts the task and resets the form; invalid title/date shows server field errors; today is computed from UTC for client guidance.

- [ ] **Step 4: Implement responsive list and creation UI**

Use a compact card/table layout that works at narrow widths, visible status labels, a native date input, and keyboard-accessible controls.

- [ ] **Step 5: Verify and commit**

Run `npm test -- --watch=false` and `npm run build`; expect PASS. Commit with `feat: add task list and creation UI`.

### Task 11: Complete Angular replacement and deletion

**Files:**
- Modify: `frontend/taskboard-web/src/app/core/tasks/task.service.ts`
- Modify: `frontend/taskboard-web/src/app/features/tasks/task-board/*`
- Modify: `frontend/taskboard-web/src/app/features/tasks/task-form/*`
- Modify: colocated specs

**Interfaces:**
- Produces: `TaskService.get`, `replace`, and `delete`; complete UI CRUD with explicit destructive confirmation and recoverable error states.

- [ ] **Step 1: Write failing service tests**

Assert exact GET/PUT/DELETE URLs, full replacement body, `204` delete handling, and no owner ID.

- [ ] **Step 2: Implement service methods**

Keep API errors typed enough for components to distinguish validation, `401`, and `404`; let the auth layer handle `401` by clearing session and routing to login.

- [ ] **Step 3: Write failing component tests**

Assert edit form prefill, free status transitions, successful replacement, unchanged-overdue-date submission, delete confirmation/cancel, persistent removal after success, `404` refresh behavior, and disabled duplicate submissions.

- [ ] **Step 4: Implement edit/delete interactions and final responsive states**

Preserve user input after validation errors. Use a simple inline confirmation rather than adding a dialog library.

- [ ] **Step 5: Verify and commit**

Run `npm test -- --watch=false` and `npm run build`; expect PASS. Commit with `feat: complete Angular task CRUD`.

### Task 12: Verify the complete demo and finish evidence documentation

**Files:**
- Modify: `README.md`
- Modify: `docs/ai-usage.md`
- Modify: `docs/presentation.md`
- Add only if corrections are found: focused regression tests and their owning source files

**Interfaces:**
- Consumes: all previous slices.
- Produces: reproducible setup/test/demo instructions, truthful AI evidence, and an English presentation outline matching the implemented system.

- [ ] **Step 1: Run the complete automated verification from a clean local state**

Run `bash scripts/check-prereqs.sh`, start PostgreSQL, run `dotnet test TaskBoard.slnx`, run Angular tests/build, and capture exact pass/fail counts in `docs/ai-usage.md`. Fix introduced failures with a failing regression test before code changes.

- [ ] **Step 2: Run the API demo path**

Verify ping, seeded login, `/me`, empty/seeded list, create, read, replace, complete, delete, invalid past date, invalid JWT, duplicate registration, and two-user `404` isolation. Record commands and outcomes without secrets/tokens.

- [ ] **Step 3: Inspect the Angular app visually**

At desktop and narrow mobile widths, verify login, registration, all CRUD, loading/empty/error states, keyboard use, and a clean browser console. Add regression tests for any corrected behavior.

- [ ] **Step 4: Complete documentation from actual evidence**

Add exact startup/reset/test commands and demo credentials to README. In `docs/ai-usage.md`, record the actual prompt, a short real generated-code excerpt with file/commit, validation, corrections, and verification. Update presentation sections to match actual architecture and trade-offs.

- [ ] **Step 5: Final verification and commit**

Rerun `dotnet test TaskBoard.slnx`, Angular tests, and Angular production build after documentation/corrections. Commit with `docs: finalize TaskBoard demo evidence` only when all reported commands have fresh evidence.
