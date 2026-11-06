# Task Rehydration Validation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ensure `TaskItem.Rehydrate` rejects invalid persisted task data without rejecting overdue tasks or altering persisted text.

**Architecture:** Keep validation in the Domain entity and retain the existing repository-facing method signature. Add validation specific to rehydration so persistence data is checked without applying time-dependent creation rules or write-time normalization.

**Tech Stack:** .NET 10, C# 14, xUnit

**Spec:** `docs/superpowers/specs/2026-11-06-task-rehydration-validation-design.md`

## Global Constraints

- Domain must not depend on API or Infrastructure.
- Apply TDD with a verified RED, GREEN, REFACTOR cycle.
- Keep overdue persisted due dates loadable and preserve persisted text exactly.
- Do not change SQL, repository interfaces, or API contracts.
- Do not add dependencies.

## Review Focus

- Empty task ID must fail with a stable domain validation error.
- Empty owner ID must fail with a stable domain validation error.
- Blank or oversized persisted title must fail without silently normalizing it.
- Oversized persisted description must fail without silently normalizing it.
- Undefined persisted status must fail while an overdue persisted date remains valid.

---

### Task 1: Validate Task Rehydration

**Files:**
- Modify: `tests/TaskBoard.UnitTests/Domain/TaskItemTests.cs`
- Modify: `src/TaskBoard.Domain/Tasks/TaskItem.cs`

**Interfaces:**
- Consumes: existing `TaskItem.Rehydrate(Guid, Guid, string, string?, TaskStatus, DateOnly?)`
- Produces: the same signature with domain validation for persisted values

- [ ] **Step 1: Add failing rehydration validation tests**

Add focused tests asserting `DomainValidationException` and the appropriate code/field for empty IDs, blank and oversized titles, oversized descriptions, and undefined status. Retain the existing test proving overdue dates and persisted text are preserved.

- [ ] **Step 2: Run the focused tests and verify RED**

Run: `dotnet test tests/TaskBoard.UnitTests/TaskBoard.UnitTests.csproj --no-restore --filter 'FullyQualifiedName~TaskItemTests.Rehydrate'`

Expected: the new invalid-data tests fail because `Rehydrate` currently accepts every value.

- [ ] **Step 3: Implement minimal rehydration validation**

In `TaskItem.Rehydrate`, validate non-empty `id` and `ownerId`, validate title without changing it, validate description length without changing it, and reuse `ValidateStatus`. Do not call `ValidateDueDate` and do not normalize persisted text.

- [ ] **Step 4: Run focused and complete verification**

Run the focused test command, then:

- `dotnet test tests/TaskBoard.UnitTests/TaskBoard.UnitTests.csproj --no-restore`
- `dotnet build src/TaskBoard.Api/TaskBoard.Api.csproj --no-restore --disable-build-servers -m:1`

Expected: all tests pass and the build succeeds with no errors.

- [ ] **Step 5: Commit the focused change**

Commit the tests, domain implementation, design, and plan with a descriptive local commit message.
