# PostgreSQL initialization

The schema file is intentionally not generated before the architecture is approved.

During implementation:
- Add a versioned `001_schema.sql` creating `users` and `tasks`, indexes, constraints and keys.
- Seed a demo user with **hashed** password via the application in Development only, plus sample tasks.
- Keep local-only database credentials out of any production deployment.

SQL files in this directory run automatically only when Docker creates the database volume for the first time. Do not edit an already-executed SQL file and expect it to rerun. For local reset, see the root README.
