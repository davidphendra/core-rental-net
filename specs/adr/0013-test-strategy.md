# ADR-0013: Test strategy — real databases, real server, no mocks

- **Status:** Accepted (2026-09-10)
- **Decided by:** product owner (E2E constraints), architecture (tooling)

## Context
The requirement is explicit: unit tests for all identified functionalities, and
E2E covering full user interaction with **no mocks and no intercepted API
calls**.

## Decision
- **Unit:** xUnit v2 (`xunit` 2.9.3) + `AwesomeAssertions` 9.6.0, one project per
  module. No mocking library — if a domain test needs a mock, the seam is wrong.
  Assertion library chosen deliberately: FluentAssertions 8.x ships a
  **non-commercial** licence and would otherwise have been pulled in silently.
- **Integration:** a **real SQLite file per test**, never the EF InMemory
  provider. InMemory would have hidden the schema, `decimal` collation and
  rowversion findings recorded in ADR-0002/0003 — the tests would pass while the
  shipped provider misbehaved.
- **E2E:** the real app as a **Kestrel subprocess on an ephemeral port**, a
  per-run throwaway database migrated and seeded through the app's own path,
  driven by **Playwright over a real socket**. No `RouteAsync`, no stubs, no
  network interception. Chromium headless plus one mobile-viewport project,
  because the mobile bottom nav and collapsed Builder panel are distinct code
  paths.
- **Architecture tests** (`NetArchTest`): no module references another's Domain
  or Infrastructure, one `DbContext` per persisting module, every table name
  carries its module prefix, Domain references no EF or ASP.NET types.
- **Traceability:** `specs/test-matrix.md` enumerates capabilities and scenarios
  and records the layer covering each. Traceability is by hand — deliberately no
  build gate.
- Component tests (bUnit) are **not** in scope; E2E covers the funnel.

## Consequences
The suite is hermetic and offline-capable. It is slower than an in-memory
approach, and that is the point.
