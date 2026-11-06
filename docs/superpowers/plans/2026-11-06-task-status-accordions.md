# Task Status Accordions Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Group task cards into independently collapsible Pending, WIP, and Completed native accordions that are open by default.

**Architecture:** Keep the API, domain model, task service, and mutation flow unchanged. Derive three ordered status groups from the existing Angular task signal and render each with semantic `<details open>` and `<summary>` markup.

**Tech Stack:** Angular 22 standalone components, TypeScript signals/computed state, semantic HTML, SCSS, Vitest.

**Spec:** `docs/superpowers/specs/2026-11-06-task-status-accordions-design.md`

## Global Constraints

- Statuses remain exactly `Pending`, `Wip`, and `Completed`; display `Wip` as `WIP`.
- Groups render in Pending, WIP, Completed order and remain visible when empty.
- All groups are open initially and collapse independently through native browser behavior.
- Do not persist open state, add expand-all controls, or change API/domain/database behavior.
- Preserve edit, delete, error, responsive, and owner-isolation behavior.
- Follow RED-GREEN and commit only after the complete verification passes.
- Do not stage `frontend/taskboard-web/angular.json` or either `.gitignore`.

## Review Focus

- A status with zero tasks still renders its accordion and a group-specific empty message.
- Initial render exposes exactly three open native details elements with semantic summaries.
- Creating, replacing, or deleting a task updates group counts without duplicate cards.
- A status-changing replacement moves the card from its old group to its new group immediately.
- Updating task data does not recreate another status's tracked details element, so a user-collapsed group stays collapsed.

---

## Planned file changes

```text
frontend/taskboard-web/src/app/features/tasks/task-board/
  task-board.ts       derive stable status groups
  task-board.html     render native details/summary groups
  task-board.scss     style accordion headings, empty states, and responsive cards
  task-board.spec.ts  pin grouping, counts, semantics, movement, and collapse preservation
```

### Task 1: Group task cards in native status accordions

**Files:**
- Modify: `frontend/taskboard-web/src/app/features/tasks/task-board/task-board.ts`
- Modify: `frontend/taskboard-web/src/app/features/tasks/task-board/task-board.html`
- Modify: `frontend/taskboard-web/src/app/features/tasks/task-board/task-board.scss`
- Test: `frontend/taskboard-web/src/app/features/tasks/task-board/task-board.spec.ts`

**Interfaces:**
- Consumes: `TaskBoardComponent.tasks: Signal<TaskItem[]>` and exact `TaskStatus` values.
- Produces: `TaskBoardComponent.taskGroups`, an ordered computed collection of `{ status, label, tasks }` for Pending, WIP, and Completed.

- [ ] **Step 1: Write failing grouping and native-semantics tests**

Add component tests that load one task in each status and assert:

- exactly three `details.status-group` elements exist in Pending, WIP, Completed order;
- every details element has `open === true` on initial render;
- each `summary` contains the display label and count;
- a group with no tasks remains visible and shows its empty message;
- closing one details element does not close the others.

- [ ] **Step 2: Run the focused test and confirm RED**

Run: `npm test -- --watch=false --include='src/app/features/tasks/task-board/task-board.spec.ts'`

Expected: FAIL because the flat task grid has no status-group details elements.

- [ ] **Step 3: Implement the minimal grouped rendering**

In `task-board.ts`, add a focused `TaskStatusGroup` interface and ordered computed `taskGroups`. In the template, track groups by `status` and render native `<details class="status-group" open>` with a `<summary>`, count, empty message, and the existing task cards/actions. Do not add accordion event state.

- [ ] **Step 4: Write the failing movement and identity regression test**

Load a Pending task, collapse the WIP details element, replace the task as Completed, and assert the card appears only under Completed, counts become `0/0/1`, and the same WIP details element remains collapsed.

- [ ] **Step 5: Run the focused test and confirm RED, then make grouping reactive**

Run the focused command from Step 2. Expected: FAIL before the computed grouping/mutation rendering is complete, then PASS after the minimal implementation.

- [ ] **Step 6: Style and visually inspect the result**

Add SCSS for clear summary affordance, status-colored accents, empty groups, card spacing, keyboard focus, and narrow screens. Inspect authenticated seeded data at desktop and 390 by 844; verify all groups start open, collapse independently, and the browser warning/error console is empty.

- [ ] **Step 7: Run full verification**

Run:

```bash
cd frontend/taskboard-web
npm test -- --watch=false
npm run build
```

Expected: all Angular tests pass and the production build completes.

- [ ] **Step 8: Commit only verified feature files**

```bash
git add frontend/taskboard-web/src/app/features/tasks/task-board/task-board.ts \
  frontend/taskboard-web/src/app/features/tasks/task-board/task-board.html \
  frontend/taskboard-web/src/app/features/tasks/task-board/task-board.scss \
  frontend/taskboard-web/src/app/features/tasks/task-board/task-board.spec.ts
git commit -m "feat: group tasks by status accordions"
```
