# TaskBoard - Codex + Superpowers challenge starter

**Status: preparation kit, not an implemented application.** This repository is intended to be opened in the Codex macOS app. Superpowers handles brainstorming, plans, TDD and reviews.

## Important constraint

The original assignment **explicitly prohibits Entity Framework, Dapper and mediator libraries**. This starter therefore uses **PostgreSQL + raw Npgsql**, not EF Core. Introducing EF Core would make the submission noncompliant, even though it could simplify CRUD.

## Stack

- .NET 10, ASP.NET Core Web API controllers (four projects: Domain, Application, Infrastructure, Api)
- PostgreSQL 17 via **Npgsql**, with parameterized SQL
- Angular 22 with standalone components, TypeScript and SCSS
- xUnit, Moq, ASP.NET Core API integration tests and PostgreSQL integration tests (Testcontainers)
- JWT bearer authentication and ASP.NET Core password hashing
- Docker Compose for local PostgreSQL

## 1. Check your Mac

Install .NET SDK 10, Docker Desktop, Git, and a Node version supported by Angular 22. Angular 22 requires Node `^22.22.3`, `^24.15.0`, or `^26.0.0` (check https://angular.dev/reference/versions for updates). Run:

```bash
bash scripts/check-prereqs.sh
```

## 2. Initialize the repository

```bash
cd ~/Projects/taskboard-challenge
git init
git add .
git commit -m "chore: initialize challenge starter"
```

In the **Codex app**, open this directory as a local project. Install **Superpowers** via `Plugins` > `Coding` > `Superpowers` (`+`). Start a new conversation after installation. Codex automatically reads `AGENTS.md` from the project root; do not paste its contents into every prompt.

## 3. Start with design, then scaffold

1. Paste `prompts/01-design.md` into Codex. It requests a design, not application code.
2. Review the design. Once approved, send `prompts/02-plan.md`.
3. When ready to implement, allow Codex to run `bash scripts/bootstrap.sh` **once** and follow `prompts/03-implement.md`.
4. Use `prompts/04-review.md` before submission.

The initial prompts intentionally do **not** repeat AGENTS.md or personal time budgets. Project docs supply context; Superpowers supplies workflow.

## 4. PostgreSQL locally

```bash
docker compose up -d
docker compose ps
```

Development connection string (local only):

```text
Host=localhost;Port=5433;Database=taskboard;Username=taskboard;Password=taskboard_dev_only
```

The `db/init/` folder is initially empty of SQL intentionally. After design, Codex should create a versioned schema script and a Development-only seed mechanism. PostgreSQL initialization scripts run **only for a new database volume**. To reset **this project's database only**, use `docker compose down -v`, then `docker compose up -d`.

## 5. Expected commands after implementation

```bash
dotnet test TaskBoard.slnx
dotnet run --project src/TaskBoard.Api
cd frontend/taskboard-web && npm run build
```

Exact application startup commands, auth configuration and demo credentials must be completed in this README once implementation exists. Do not present this starter as a functioning app.

## Documentation

- `docs/assignment-original.md`: original brief, including restrictions
- `docs/requirements.md`: actionable acceptance criteria
- `docs/decisions.md`: architecture decisions and trade-offs
- `docs/user-story.md`: initial user story and suggested acceptance criteria
- `docs/ai-usage.md`: evidence of actual AI prompts, output, validation and corrections
- `docs/presentation.md`: English presentation structure

## Notes

- ASP.NET Core controller-based Web API plus Angular is our interpretation of the ambiguous "MVC" wording. No Razor views unless specifically required by the panel.
- Do not fabricate test runs, RED/GREEN evidence, generated code samples, or seeded users.
- No remote Git push or deployment is authorized by this starter.
