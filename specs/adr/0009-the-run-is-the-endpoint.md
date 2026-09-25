# 0009 — The run IS the endpoint

**Status:** accepted
**Date:** 2026-09-25
**Deciders:** product owner, engineer

## Context

Three changes had already stripped the run down: [0006](0006-the-run-endpoint-is-a-controller.md) made the
endpoint a controller, [0007](0007-the-run-is-not-guarded-or-validated.md) removed the guard and the validator,
and [0008](0008-the-run-has-no-deadline.md) removed the deadline. What was left was `SuggestionRun` — a
per-run object the action constructed and handed everything to, holding the ledger, the field reader, the
event switch and the frame mapping, with a single public entry point, `RunAsync`.

The ask: merge `RunAsync` into `Suggest`, which dissolves the type — `RunAsync` was its only way in.

**The repository's own budgets are what shaped the answer**, and they measure two different things:

| Budget | Rule | What it counts |
|---|---|---|
| File | ≤ 150 code lines (`CodingStandardTests`) | non-blank, non-comment lines |
| Method | ≤ 40 lines | **raw lines** — in-body comments and blanks included |

Measured before the merge: `SuggestionRun.cs` **143** code lines, `BuilderController.cs` **49**. A straight merge
lands near 190 in one file — over the file budget by a wide margin — and the action's own body, carrying
`RunAsync`'s reasoning comments, ran to **54 raw lines** against a 40-line cap.

## Decision

**D1 — `SuggestionRun` is deleted; its orchestration is the action.** `Suggest` now refuses an unusable body,
reads the customer's token, names the customer, opens the ledger, runs, and writes the record — the whole of what
`RunAsync` did, with no object between the request and the run.

**D2 — The run's state is held in controller fields.** `NarrativeFieldReader` and `RunLedger` were constructor
injected into `SuggestionRun`, i.e. one pair per run. As fields on the controller they are **still one pair per
run**, because MVC creates a controller instance per request; the class remark says so, since "a field on a
controller" otherwise reads as shared state. Neither is static, and nothing is shared between two runs.

**D3 — The answer-to-frame mapping moved to `SuggestionResultFrame.From`.** It was `Frames` plus `CandidateOf`
inside the run: ~30 code lines that translate `AgentSuggestionResult` into the frame the browser reads. The frame
is the shape being translated *into*, so the mapping belongs with it — and moving it is also what brings the
merged controller inside the file budget. The one rule that travels with it is that an answer claiming a
suggestion and carrying no option becomes `null` rather than an empty result.

**D4 — The narrative-field writing is inlined into the event handling.** `WriteFieldsAsync` was a three-line
method called from one place; its loop now sits in the `NarrativeDelta` case, where the comment about partial
values already was.

**D5 — `FailBestEffortAsync` is deleted, and the second cancellation catch no longer writes anything.** This
**narrows [0008](0008-the-run-has-no-deadline.md) D4**, which kept that write. Two findings decided it:

1. *The path is unreachable through the agent.* `FoundrySuggestionAgent.Relay` rethrows an
   `OperationCanceledException` **only** when the caller's token is signalled and catches every other exception
   into an `Unavailable` event — so no non-customer cancellation can escape the adapter, and with the deadline
   gone ([0008](0008-the-run-has-no-deadline.md)) nothing else in the application raises one either.
2. *Reaching the stream from a catch costs more than the code is worth.* The stream is opened inside the `try`,
   so a `catch` cannot see it: writing there would mean either a nullable `_stream` field that is null for part of
   one method, or a `stream` parameter the catch cannot be given. Both are ways of making dead code compile.

The catch survives as a **record-only** guard — a non-customer cancellation is still recorded as
`AiRunVerdict.Unavailable` rather than mistaken for a customer's stop — and its reasoning lives in the action's
remarks.

**D6 — The action no longer opens the stream; `WriteAsync` does.** Opening it is the point of no return, and it
belongs to the thing that writes the run rather than to the action that decides how a run *ends*. It also keeps
the action's two endings about the run rather than about the response, and buys back the raw lines D1 spent.
*(Reversed by [0010](0010-the-run-is-one-method.md), which merged `WriteAsync` into the action — the same
trade, decided the other way.)*

**D7 — The record is still written in a `finally`.** A run with no record is exactly the run nobody can explain,
including one the customer stopped.

## Consequences

**Good**

- One fewer type and one fewer construction site: the endpoint is the action plus private methods, and there is
  no per-run object whose lifetime a reader has to work out. (It was four private methods when this was written;
  [0010](0010-the-run-is-one-method.md) merged `WriteAsync` into the action and
  [0011](0011-the-payload-is-built-by-the-endpoint.md) merged the request builder in.)
- The frame is built where the frame is defined, so the translation from the agent's answer to the page's shape
  has one home.
- `SuggestionResultFrame` is 32 code lines and `BuilderController` 128 — both comfortably inside the file budget.

**Bad / recorded**

- **`BuilderController` is now the largest file in `src`, and the action has almost no headroom**: `Suggest` was
  **36 of 40 raw lines** when this was written, and [0010](0010-the-run-is-one-method.md) has since taken it to
  **37**. Because the method budget counts raw lines, *adding a two-line comment inside the action fails the
  build* — the reasoning that would have sat there had to move into the action's remarks, which are above the
  signature and therefore not counted. Anyone touching this file should know that before they add a comment, and
  should know that the file budget and the method budget count differently.
- **A transport cancellation is no longer reported to the page** (D5). The record carries it and the stream
  simply ends. The path is unreachable through the agent today; if a future adapter can produce it, the customer
  will see a run stop with no reason and the page will word it as an ending.
- **`SuggestionResultFrame` now references the agent's types**, so the browser's shape knows the model's shape.
  That is the trade for D3, and it is one direction only — nothing in `Agents/` references a frame.
- **Nothing here is verified at the browser tier**, which is red at HEAD for the reason
  [0006](0006-the-run-endpoint-is-a-controller.md) D6 gives.

## Alternatives rejected

| Alternative | Why rejected |
|---|---|
| Keep `SuggestionRun` and merge only its orchestration into the action | Leaves a type whose only entry point has been taken away: private helpers with no public member |
| Extract a new type for the streaming half | Reintroduces exactly the type the ask removes, under another name |
| Let the merged file breach the 150-code-line budget | `CodingStandardTests` fails, and a green baseline is this repository's definition of done |
| Hold the stream in a nullable field so a catch could write to it | A field that is null for part of one method, existing only to make unreachable code compile |
| Move the event switch onto `SuggestionEventStream` | That type's job is framing values on the wire and its remark says every value it writes is one the run has already checked; giving it the agent's vocabulary makes it the run by another name |
| Compress the action by deleting its in-body comments without lifting the reasoning | The repository's comments carry *why*; the reasoning moved to the remarks rather than being dropped |

## Baselines

Measured on 2026-09-25, in the same working tree as [0006](0006-the-run-endpoint-is-a-controller.md),
[0007](0007-the-run-is-not-guarded-or-validated.md) and [0008](0008-the-run-has-no-deadline.md).

| Baseline | Before this change | After |
|---|---|---|
| `BUILD` | `dotnet build CoreRentalNet.sln` — **0 warnings, 0 errors** | **0 warnings, 0 errors** |
| `NONBROWSER` | **712 passed, 0 failed** | **712 passed, 0 failed** (no test added or removed) |
| `BROWSER` | **red at HEAD**: 118 failed, 1 passed, 1 skipped — `Not built: …CoreRentalNet.E2E.LocalAgent.dll` | **unchanged**, same cause |
| `AGENT` | not run; nothing under `agentfoundry/` is touched | **40 passed, 0 failed** |

## Notes

- **The two budgets count differently, and that surprised this change.** The file budget excludes comments; the
  method budget does not. `Suggest` was 54 raw lines before the in-body comments were lifted into its remarks —
  the same code, over the limit, because of prose. That is worth knowing for any file in this repository that
  argues at length inside a method.
- **The refactor is behaviour-preserving except for D5**, which was already unreachable. The frames, their order,
  their names, the stages, the verdicts and the record are unchanged.
- **One thing this makes awkward, recorded rather than hidden.** With the run inside the endpoint, the only way
  to exercise any of it is over HTTP. `BuilderApiTests` covers the route, the gate, the 200 stream, the two 400
  refusals and — until [0007](0007-the-run-is-not-guarded-or-validated.md) — the 409. A mid-run stop, the
  narrative-field splitting and the verdicts now have no unit-level seam at all.
