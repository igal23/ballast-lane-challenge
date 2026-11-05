# Architecture decision log

## ADR-001: Single controller-based API host
One ASP.NET Core host exposes authentication and task endpoints. Domain has no outward references; Application depends on Domain; Infrastructure implements Application abstractions; Api composes the application. No additional services or mediator framework.

## ADR-002: PostgreSQL + Npgsql (no ORM)
**Assignment constraint:** Entity Framework, Dapper and mediator libraries are prohibited. Use `NpgsqlDataSource`, `NpgsqlCommand` and parameterized queries with explicit mapping. Trade-off: more persistence code than EF Core, but predictable SQL, no forbidden dependencies and easy DB-specific integration tests.

## ADR-003: Angular 22 SPA
Use standalone Angular components, TypeScript, built-in HTTP client and a small auth/task service layer. Avoid NgRx and SSR for this small application.

## ADR-004: JWT and task ownership
Use the standard JWT bearer middleware. Hash passwords using an established ASP.NET Core hasher. Resolve user identity from validated token claims (not user-supplied payload fields); scope all queries and mutations to the current user's ID. Return 404 for inaccessible task IDs.

## ADR-005: MVC interpretation
The brief mentions ASP.NET MVC and Web API and also requires a frontend framework. We interpret MVC as ASP.NET Core MVC controllers used to implement the API; Angular provides views. No Razor UI unless explicitly clarified.

## ADR-006: Honest evidence-driven testing
Write tests before behavior where feasible; preserve actual RED/GREEN proof. Unit-test business rules. Exercise SQL against PostgreSQL in integration tests; use API integration tests for auth and HTTP semantics. Document skipped or blocked tests rather than treating them as passing.

## ADR-007: Explicit use-case classes without a mediator
Use one small Application class for each authentication or task operation. API controllers invoke these classes directly through dependency injection. This adds some types but keeps behavior, dependencies and tests explicit. Do not introduce a mediator package, generic handler pipeline or dispatch abstraction.
