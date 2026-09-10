# E8 — CI and documentation

**Slice:** 3 · **Status:** pending · **ADR:** 0014, 0015

## Goal
Make the suite run on every push, and leave a repository a reader can start.

## In scope
- GitHub Actions workflow: restore, build with warnings-as-errors, unit tests,
  architecture tests, integration tests, then E2E with
  `playwright install --with-deps chromium`. **Dormant until a remote exists**;
  committed now so it starts running with no changes once the repo is pushed.
- Root `README.md`: how to run it, the demo phrase, how to reset the database file
  (required, because the catalog is loaded once at startup and the database is
  disposable), the epic map, and where `specs/` lives.
- `specs/epics/archive/index.md` entries maintained on each archive: epic,
  completion date, commit SHA, test counts, and anything deliberately left out.

## Out of scope
Deployment, containers, any Azure resource, and any cloud provisioning.
Deployment is deferred and requires explicit consent (ADR-0014).

## Stories
1. As the product owner I want CI to run the mock-free suite, because that is the
   only thing that keeps it honest.
2. As a reader I want to start the app from the README without guesswork.
3. As the product owner I want archived epics to record what was and was not done.

## Definition of done
Workflow committed and locally validated; README written; archive index current.

## Risks
- A CI workflow that has never run is an unverified claim. Until a remote exists
  the README will say so explicitly rather than implying CI is green.
