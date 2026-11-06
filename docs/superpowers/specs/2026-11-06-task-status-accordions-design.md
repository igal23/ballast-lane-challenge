# Task status accordions design

## Intent

Make the task board easier to scan by grouping task cards under their exact domain statuses: Pending, WIP, and Completed.

## Goals

- Render one status section for Pending, WIP, and Completed, in that order.
- Show the number of tasks in each section heading.
- Keep all three sections open on initial render.
- Let users expand or collapse each section independently.
- Preserve the current task card edit/delete behavior and responsive layout.
- Move a task to the correct section immediately after creation or replacement.
- Keep each section's existing due-date and UUID ordering.
- Use semantic, keyboard-accessible markup without adding a UI dependency.

## Non-goals

- Persisting accordion state across navigation or sessions.
- An expand-all or collapse-all control.
- Drag-and-drop or status changes from the section heading.
- API, domain, database, or authentication changes.

## Approach

Use three native `<details open>` elements with `<summary>` headings. Each summary contains the display label and count. The component derives status groups from its existing task signal, so task mutations automatically render in the correct group. Empty groups remain visible and display a short empty-state message.

The alternative considered was Angular-controlled accordion state using signals. It would support persistence or coordinated expand/collapse behavior, but adds state and event handling that this independently collapsible design does not need. Native details were selected for simpler code and built-in accessibility.

## Validation

Component tests will first fail, then verify status grouping, counts, default-open state, empty groups, semantic details/summary markup, and movement after a status-changing replacement. The complete Angular suite and production build must pass, followed by desktop and narrow mobile visual inspection.
