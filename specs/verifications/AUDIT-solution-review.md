# Audit code — Round 1 self-review

Skill: `audit-code`, checklist and
[`HEURISTICS.md`](../../../.pi/agent/skills/audit-code/HEURISTICS.md). Date: 2026-09-16.
Mode: full checklist. Scope: the solution-review round-1 change set plus the repository-wide gates.

## Gate summary

| Section | Result |
|---|---|
| Supply Chain & Security | **PASS** |
| Provenance & Metadata | **PASS** |
| Law of Demeter | **PASS** |
| CONVENTIONS.md Compliance | **PASS after fix** (CONVENTIONS.md was missing; created this round) |
| Scope | **PASS** |
| Boy Scout Rule | **PASS** |
| Types and Safety | **PASS** |
| Test Coverage | **PASS** |
| SOLID and Heuristics | **PASS** (one Chapter 17 note, below) |
| Refactoring Smells | **PASS** (one named smell accepted) |
| Code Style | **PASS** |

## Supply Chain & Security

- [x] No new dependencies. `Directory.Packages.props` unchanged this round.
- [x] No `[SLOP]` or unapproved packages.
- [x] No secrets in the diff. The only grep hits are `/placeholders/*.svg` in build output and a
      vendored `.claude/settings.local.json` inside gitignored `node_modules/`.
- [x] OWASP spot-check on the change: the change is a write-path/read-path split in an in-process
      Blazor application. It adds no HTTP surface, no SQL, no deserialization and no auth change.
      The one security-relevant guard — the checkout confirmation gate — is untouched and still
      covered by the behaviour lock.
- [x] No HIGH security findings.

## Provenance & Metadata

- [x] No new plan artefacts of YAML type were added; the decision records are Markdown under
      `specs/` and cite their sources inline (`specs/CQRS_DESIGN_LATEST.md` §Sources).
- [x] Implementation decisions reference the rule that drove them (each audit report cites the
      catalog section and the URL).

## Law of Demeter

- [x] No `a.B().C().DoX()` chains introduced. `WorkspaceSession` calls one handler, then one reader.
- [x] `PermissionClaims.From` is the one chain of note (`new JsonWebToken(...).Claims.Where(...)`),
      and it is a LINQ pipeline over a local value, not a walk across collaborators. Pre-existing,
      read and kept.

## CONVENTIONS.md Compliance

- [x] All output files are under `specs/` (`CQRS_DESIGN_LATEST.md`, `MODULE_AUDIT_LATEST.md`,
      `LAYER_AUDIT_LATEST.md`, `CSHARP_AUDIT_LATEST.md`, `modules.map`, `verifications/`). The one
      root file added is `CONVENTIONS.md`, which the skill itself requires.
- [x] No `gh issue create` anywhere; `gh` is not invoked at all.
- [x] No GitHub REST call. The sole `api.github.com` string is in a vendored dev tool under
      gitignored `node_modules/`.

**Finding AC-1 (fixed this round): `CONVENTIONS.md` did not exist.** The skill's verify gate
(`test -f CONVENTIONS.md`) failed, and with it every checklist item that names CONVENTIONS.md. The
conventions it holds are real — they are enforced by `CodingStandardTests`, `OneTypePerFileTests`,
`ModuleBoundaryTests` and `SourceLayoutTests` — but the document was absent. `CONVENTIONS.md` is now
written from the enforced rules.

**Finding AC-2 (logged, not fixed): the authority chain in `CLAUDE.md` names four files that do not
exist** — `specs/tech-architecture/architecture-style.md`, `specs/architecture.md`,
`specs/test-matrix.md`, `specs/state.yaml`. This is the known CLN-10 gap recorded in
`specs/LAYER_AUDIT_LATEST.md`. It is a process defect, not a code defect, and restoring the files is
outside a code audit. Logged.

## Scope

- [x] Changes are limited to the review: five handler interfaces, five handlers, `WorkspaceSession`,
      four field renames, `<inheritdoc />` additions, one new architecture test, one new
      `CONVENTIONS.md`, and the audit/decision documents under `specs/`.
- [x] No speculative features.
- [x] No files touched outside the stated scope. `CatalogBrowser` and `ProductCatalog` were opened for
      the field-naming rule (the Boy Scout Rule applies to files already in the audit).

## Boy Scout Rule

- [x] Every file touched is cleaner: the handlers lost a dependency and a read-model return; the four
      fields follow the field convention; `WorkspaceSession` re-reads instead of trusting a write
      return; `CONVENTIONS.md` now exists.
- [x] No dead code left behind. `grep` for committed-out code (`// var`, `// if`, `TODO`, `FIXME`,
      `HACK`) in `src/` returns nothing.
- [x] No commented-out code blocks.

## Types and Safety

- [x] Nullable enabled; warnings as errors across the solution.
- [x] No `!` added. The two `null!` in `Rental` are pre-existing EF navigation guards.
- [x] No casts bypassing type safety.

## Test Coverage

- [x] The signature change is covered: `WorkspaceCommandTests`, `StartDraftTests`,
      `WorkspaceCompositionTests`, `EndToEndCheckoutTests`, `WorkspaceRuleLockTests` and the Host
      tests all exercise the write path, and all pass unchanged in behaviour.
- [x] The new production rule (Application must not name a framework) has a new regression guard,
      `ApplicationDependencyTests`, non-vacuous because it asserts four assemblies were discovered.
- [x] Tests verify behaviour through public interfaces.
- [x] F.I.R.S.T.: the whole non-browser suite is 482 tests in under 3 seconds.

## SOLID and Heuristics

- [x] Single Responsibility: the write handlers now do one thing (mutate); the read path does one
      thing (project).
- [x] Open/Closed, Dependency Inversion: unchanged and still interface-first.
- [ ] **Chapter 17 note — G30 (functions should do one thing) and CS3 from the C# audit.**
      `PlaceOrderService` (10 dependencies) and `RentalScheduler` (9) are single-operation
      transaction coordinators. Their width is the transaction's collaborators, not two
      responsibilities. Recorded as OPEN in `specs/CSHARP_AUDIT_LATEST.md`; resolution needs a
      product-owner decision.

## Refactoring Smells (Fowler)

- **Middle Man — dismissed.** `MutateAsync` and `AttemptAsync` both forward to a handler, but each
  names a concept the callers need (the session's refusal rule; re-read after write), so neither is a
  pass-through.
- **Primitive Obsession — dismissed.** `string Sku`, `string draftToken` and `int Quantity` are
  primitives at the handler boundary; inside the module they are wrapped (`DraftToken`, `SlotId`).
  Widening that boundary would be a new abstraction with no demand.
- **Feature Envy, Data Clumps, Message Chains, Mysterious Name** — none found in the change set.

## Code Style (CONVENTIONS.md)

- [x] Functions 4–20 lines; the largest changed method, `AttemptAsync`, is 12 lines.
- [x] Stepdown Rule holds.
- [x] Files under 300 lines. `WorkspaceSession` is ~120.
- [x] Names are specific; no new name collides.
- [x] No duplication introduced. The one duplication this round *removed* was the read-model build
      that lived in five handlers and the view service.
- [x] Early returns; ≤ 2 levels of indentation.
- [x] Conditionals positive where the language allows. Two negative reads remain and are idiomatic:
      `if (!alreadyConverted)` guards a state check, `if (!outcome.DidNothing)` guards a log line.
- [x] Comments explain why. `WorkspaceSession`'s remarks state why the re-read happens; the handler
      interfaces state why they return nothing.

## Red flags — rationalizations caught

None skipped silently. Two items are deliberately **not** fixed and are stated rather than hidden:

1. **CS3, the wide constructors.** I could have relabelled them LOW to clear the gate. I did not.
   They stay HIGH and OPEN.
2. **AC-2, the missing authority-chain files.** I could have edited `CLAUDE.md` to delete the dead
   links and made the symptom disappear. I did not: the files are part of the process contract, and
   removing the references would hide the gap rather than close it.

## Round log

Rounds 2–5 re-ran this checklist. It stayed PASS. The changes they made were: splitting
`RentalScheduler` into `RentalScheduler` + `RenewalInvoiceIssuer` (round 2), documenting the number
value types (round 2), and splitting `CheckoutCommandHandler` into `CheckoutCommandHandler` +
`CheckoutConfirmation` (round 3). All three follow this checklist's own rules: one public operation
per service, constructor injection, no speculative abstraction, one type per file.

Final baselines: BUILD 0 warnings / 0 errors, NONBROWSER **482 passed**, BROWSER **103 passed, 1
skipped**. Rounds 4 and 5 found nothing new, so the loop converged.

The one checklist item that cannot be marked honestly PASS is the Chapter 17 heuristic note (CS3,
`PlaceOrderService`). It is stated as an open HIGH in `specs/CSHARP_AUDIT_LATEST.md`, and a
product-owner decision is requested rather than assumed.

The full loop record is in `specs/REVIEW_ROUNDS.md`.
