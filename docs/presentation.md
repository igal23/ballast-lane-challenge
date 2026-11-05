# English technical panel - presentation outline

Use as a speaking guide, not a full slide deck. Target a concise introduction and demo, leaving substantial time for live code review and questions.

## 1. User story and scope
- Who uses TaskBoard and what does it solve?
- Show accepted versus intentionally omitted features.

## 2. Architecture and design decisions
- Project dependency direction: Api -> Application -> Domain; Infrastructure -> Application.
- Why single API host and controller-based MVC.
- Why PostgreSQL + raw Npgsql; cite prohibition of EF/Dapper.
- How data access, validation, auth and persistence are separated.

## 3. Demonstration
- Start API and Angular UI with seeded Development data.
- Login, list, create, edit and delete tasks.
- Show public vs protected endpoint behavior.
- Demonstrate cross-user isolation or a corresponding integration test.

## 4. Testing and TDD
- Show a real failing test, subsequent code and passing result or git history.
- Explain unit vs integration tests and actual PostgreSQL coverage.

## 5. Critical GenAI usage
- Show a real prompt, generated code sample and an issue you identified.
- Explain how you corrected it and verified the improvement.

## 6. Trade-offs and follow-ups
- Mention what you would improve with more scope (e.g., refresh token strategy, pagination, migrations).
- Be ready to explain SQL parameterization, JWT, password hashing, HTTP response semantics and DI.
