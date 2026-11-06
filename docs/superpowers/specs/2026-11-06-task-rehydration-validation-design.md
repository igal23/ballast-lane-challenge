# Task Rehydration Validation Design

## Intent

Prevent `TaskItem.Rehydrate` from creating domain objects with invalid persisted data while preserving the existing ability to load overdue tasks and retaining stored text exactly as persisted.

## Goals

- Reject empty task and owner identifiers during rehydration.
- Reject blank or oversized titles, oversized descriptions, and undefined statuses.
- Preserve persisted title and description text rather than normalizing it during reads.
- Continue allowing historical due dates because a valid task naturally becomes overdue over time.
- Reuse the existing domain validation exception and error codes where they already apply.

## Non-goals

- Do not change create or replace behavior.
- Do not change repository interfaces, SQL, database constraints, or API contracts.
- Do not add dependencies or introduce a persistence-specific domain factory.

## Approaches

1. **Validate within `Rehydrate` (selected).** Keep the current API and add persistence-safe validation that does not normalize text or compare the due date with today. This is the smallest change and keeps invariants in the domain.
2. Make `Rehydrate` internal and expose internals to Infrastructure. This narrows visibility but creates assembly coupling and still requires validation for corrupt persisted data.
3. Route rehydration through `Create`. This reuses validation but incorrectly rejects overdue tasks and changes persisted text through normalization.

## Verification

Add domain tests proving invalid identifiers, titles, descriptions, and statuses are rejected, while the existing overdue-date preservation test remains green. Run the complete backend unit suite and build the API project.
