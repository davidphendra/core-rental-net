# E7 — Test matrix completion and the browser suite

**Slice:** 3 · **Status:** pending · **ADR:** 0013

## Goal
Prove every capability in `specs/test-matrix.md` with a real test, and drive the
whole funnel in a real browser with no mocks and no intercepted calls.

## In scope
- Mark every matrix row `passing` or explicitly `n/a` with a reason.
- Integration tests against a **real SQLite file per test**.
- E2E infrastructure: the real app as a **Kestrel subprocess on an ephemeral
  port**, a per-run throwaway database migrated and seeded through the app's own
  path, deleted at teardown, driven by **Playwright over a real socket**.
  Chromium headless plus one mobile viewport project.
- **No `RouteAsync`, no stubs, no network interception anywhere in the suite.**
- Flows: Home → Builder → Store → Review → dialog (wrong phrase, then correct) →
  confirmation; reload mid-funnel; two-tab draft conflict; cancellation of the
  dialog; mobile bottom-nav gating; keyboard-only completion.

## Out of scope
bUnit component tests and axe-core assertions (both deliberately excluded).

## Stories
1. As the product owner I want to know that a claim in the specs is backed by a
   named test.
2. As the product owner I want the funnel exercised as a user actually walks it,
   on desktop and on a phone viewport.
3. As a developer I want a failing E2E run to be reproducible and leave no state
   behind.

## Definition of done
Every matrix row resolved; E2E green twice in a row against fresh databases;
capsule archived.

## Risks
- Flakiness from shared state: each run must get its own database file, and
  teardown must delete it.
- Third-party image requests would break hermeticity — the images are vendored
  in E3.
