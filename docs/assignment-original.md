# Original assignment (user-provided)

## Project Overview
Develop a web application with API and data layer using .NET C#, ASP.NET MVC, Web API and a database or data store, with Clean Architecture and ideally TDD. Create an informal user story for the presentation.

The app must support CRUD via API and store user registration/login information.

**Hard requirement: Do not use Entity Framework, Dapper, or Mediator.**

## Backend
- Database/data store with at least one domain table/container and one for users; each domain record has a unique ID plus at least two other fields.
- CRUD ASP.NET Web API endpoints with appropriate verbs, parameters and return values.
- A second API area for creating users, logging in, and authorized/unauthorized endpoints.
- Separate data access layer for CRUD.
- Business logic layer with all rules and validation, independent of data access and API.
- Unit tests for data, business logic, and API endpoints.

## Frontend
- Frontend framework (React, Vue, etc.); responsive and user-friendly.
- Complete domain CRUD from UI.
- Clean component/state organization.

## Submission
- README for setup and additional documentation.
- Seed data and demo credentials.

## GenAI requirement
For a task management API (title, description, status, due_date, user association):
- Provide the prompt used with the AI coding tool to generate scaffold or implementation.
- Show representative generated code.
- Explain how suggestions were validated, improved and corrected.
- Cover edge cases, authentication and validations.

## Presentation and code review
- Present user story, design choices, technical architecture, and working demo over screen sharing.
- Explain coding decisions in a follow-up live code review.
- Evaluation: Clean Architecture, test coverage and TDD, code quality, correct functionality, browser console warnings, presentation, and critical use of AI tools.
