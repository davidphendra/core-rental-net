# ADR-0003: Optimistic concurrency via an explicit int Version token

- **Status:** Accepted (2026-09-10)
- **Decided by:** architecture (verified empirically)

## Context
The workspace draft is persisted server-side and edited from two tabs. Lost
updates would surface as "my monitor disappeared".

## Decision
An explicit `int Version` property, configured `IsConcurrencyToken()`, is
incremented in `SaveChanges` for modified aggregates. Concurrent writes raise
`DbUpdateConcurrencyException`, which the host surfaces as a conflict and the UI
resolves by reloading the current draft.

**Do not use `IsRowVersion()`.** It was measured on SQLite: EF created
`"RowVersion" BLOB NULL`, never populated it, and it stayed `NULL` on every row —
zero protection with no error. An explicit token was measured to raise
`DbUpdateConcurrencyException` correctly.
