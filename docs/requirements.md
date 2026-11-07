# Actionable requirements

This is an implementation checklist, not a new specification. See `assignment-original.md` for the complete user-provided brief summary.

## Must-have

- [x] ASP.NET Core Web API controllers with complete task CRUD (`GET`, `POST`, `PUT/PATCH`, `DELETE`).
- [x] Users persist in a separate PostgreSQL table; registration/login persist and validate identities.
- [x] Task fields: primary key, title, description, status, due date and owner ID.
- [x] API includes public and protected endpoints, with appropriate response codes.
- [x] Business logic and validation independent of API and database implementation.
- [x] PostgreSQL accessed using **raw Npgsql**, without EF, Dapper or mediator libraries.
- [x] Password hashes, parameterized SQL and strict task ownership checks.
- [x] Seeded Development-only user with credentials documented in README, plus sample tasks.
- [x] Responsive Angular UI with login and CRUD; clean components/state.
- [x] Unit tests for domain/application and API/controller behavior.
- [x] Real PostgreSQL integration tests for repositories and API integration coverage.
- [x] Clear README with exact local run/test instructions.
- [x] Actual GenAI prompts, representative output, validation and corrections documented.
- [x] English presentation explaining user story, architecture, trade-offs, code, tests and AI process.

## API contract - starting point, finalize during brainstorming

- `POST /api/auth/register` (anonymous)
- `POST /api/auth/login` (anonymous)
- `GET /api/auth/me` (authorized)
- `GET /api/public/ping` (anonymous)
- `GET /api/tasks` (authorized, current user's tasks)
- `GET /api/tasks/{id}` (authorized)
- `POST /api/tasks` (authorized)
- `PUT /api/tasks/{id}` (authorized)
- `DELETE /api/tasks/{id}` (authorized)

## Required edge cases

- [x] Duplicate email handled deterministically.
- [x] Invalid password does not authenticate.
- [x] Missing/invalid JWT rejected.
- [x] Task belonging to another user cannot be viewed/modified/deleted; avoid revealing its existence.
- [x] Invalid title, status, and due date handled consistently.
- [x] Invalid or unknown task ID handled consistently.
- [x] Empty list is valid; deletion is persistent.
- [x] SQL parameters are used for ALL user-influenced values.

## Scope boundaries

- MVC means ASP.NET Core controller-based API; Angular handles frontend (no Razor unless clarified).
- No CQRS, event bus, microservice, generic repository or elaborate state management unless justified.
- Swagger UI may require a small additional package beyond built-in OpenAPI.
