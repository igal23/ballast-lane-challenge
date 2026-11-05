# Informal user story - draft

> As a registered user, I want to sign in and manage my own tasks, so that I can track work while keeping my information private from other users.

## Initial acceptance criteria

1. A visitor can register with an email and password and then log in.
2. An authenticated user can create a task with title, description, status and due date.
3. Users can list, view, update and delete their own tasks.
4. Users cannot access other users' tasks, even by guessing task IDs.
5. Invalid input returns clear, predictable errors and does not mutate persisted data.
6. A Development-only seeded user and tasks allow a repeatable demo.

Finalize task status transitions, date rules and validation lengths during Superpowers brainstorming. Do not invent business rules solely to populate a service layer.
