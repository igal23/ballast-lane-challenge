# TaskBoard

TaskBoard is an ASP.NET Core and Angular task manager backed by PostgreSQL. The backend uses Clean Architecture, raw parameterized Npgsql, JWT bearer authentication, and owner-scoped task persistence.

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

## 2. Run PostgreSQL and the API

```bash
docker compose up -d
docker compose ps
dotnet run --project src/TaskBoard.Api --launch-profile http
```

The API listens on `http://localhost:5253`. Development startup applies migrations and idempotently creates the demo data.

Development-only defaults are stored in `appsettings.Development.json`. They are intentionally public local values and must never be reused outside local development.

Demo credentials:

```text
Email: demo@taskboard.local
Password: TaskBoard demo passphrase 2026!
```

For a non-Development environment, provide configuration externally. ASP.NET Core maps double underscores to configuration separators:

```bash
export ConnectionStrings__TaskBoard='Host=localhost;Port=5433;Database=taskboard;Username=taskboard;Password=replace-me'
export Jwt__Issuer='TaskBoard.Api'
export Jwt__Audience='TaskBoard.Web'
export Jwt__SigningKey='replace-with-at-least-32-random-utf8-bytes'
export AllowedOrigins__0='https://your-frontend.example'
```

Startup fails clearly if the connection string, JWT issuer, audience, signing key, or allowed origins are missing. Production secrets must not be committed.

## 3. Run the Angular client

In a second terminal:

```bash
cd frontend/taskboard-web
npm ci
npm start
```

Open `http://localhost:4200` and sign in with the demo credentials above. The development server proxies `/api` to `http://localhost:5253`, so the API must be running. Task due dates are date-only values. The server evaluates them in UTC; UI timezone conversion is intentionally outside this MVP.

## 4. Smoke test the API

Ping and register a user:

```bash
curl -sS http://localhost:5253/api/public/ping

curl -sS -X POST http://localhost:5253/api/auth/register \
  -H 'Content-Type: application/json' \
  -d '{"email":"user@example.com","password":"a sufficiently long password"}'
```

Log in as the demo user, copy `accessToken` from the response, and set it locally:

```bash
curl -sS -X POST http://localhost:5253/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"demo@taskboard.local","password":"TaskBoard demo passphrase 2026!"}'

TOKEN='paste-accessToken-here'

curl -sS http://localhost:5253/api/auth/me \
  -H "Authorization: Bearer $TOKEN"

curl -sS http://localhost:5253/api/tasks \
  -H "Authorization: Bearer $TOKEN"

curl -sS -X POST http://localhost:5253/api/tasks \
  -H "Authorization: Bearer $TOKEN" \
  -H 'Content-Type: application/json' \
  -d '{"title":"Verify local demo","description":null,"status":"Pending","dueDate":null}'
```

## 5. Tests and build

```bash
dotnet test TaskBoard.slnx
cd frontend/taskboard-web
npm test -- --watch=false
npm run build
```

## 6. Reset only this project's database

This permanently deletes the TaskBoard Docker volume and all local TaskBoard data, then creates a fresh database that Development startup will migrate and seed:

```bash
docker compose down -v
docker compose up -d
```

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
