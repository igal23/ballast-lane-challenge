# TaskBoard - Agent Instructions

1. Follow Clean Architecture: Domain and Application must not depend on API or Infrastructure.
2. Respect assignment constraints: no Entity Framework, Dapper, MediatR, or mediator libraries. Use PostgreSQL through Npgsql with parameterized SQL.
3. Prefer correctness, simplicity, robustness, and maintainability. Avoid overengineering, speculative abstractions, and generic repositories.
4. Apply TDD for business behavior: RED, GREEN, REFACTOR. Cover meaningful validation, authorization, and edge cases.
5. Enforce password hashing, input validation, authentication, authorization, and per-user task isolation; never trust client-provided owner IDs.
6. Keep changes focused. Do not refactor unrelated code, edit generated files, or add dependencies without clear justification.
7. Reproduce bugs before fixing them; add regression tests. Inspect frontend behavior visually when tooling allows.
8. Run relevant tests and builds. Fix introduced failures and report unverified behavior rather than claiming success.
9. Make small, descriptive local Git commits. Do not add AI co-authors; never push, open PRs, or deploy without explicit authorization.
10. Consult relevant project docs, explain important trade-offs concisely, record AI-generated code corrections, and never use em dashes.
