# 0011 — The payload is built by the endpoint

**Status:** accepted
**Date:** 2026-09-25
**Deciders:** product owner, engineer

## Context

`SuggestionRequestBuilder` was the last per-run helper in the AI feature: a class with one public operation
(`Build`) and one private one (`EverySlot`), constructed from `IProductCatalogService` and
`WorkspaceSlotSettings`. Its own remark recorded why it existed at all — "the catalogue is read here for exactly
one value, the currency the request is stated in".

The ask: remove it as a `BuilderController` parameter and merge it into the controller. Five earlier changes had
already dissolved everything around it ([0006](0006-the-run-endpoint-is-a-controller.md)–[0010](0010-the-run-is-one-method.md)),
so this is the last one out.

**Nothing blocked the merge**, unlike the previous two, and it is worth saying why rather than assuming it:

| Check | Result |
|---|---|
| Visibility cascade (`CS0051`) | None. The controller is `internal`, so it may name internal types, and both of these are `public` anyway |
| Are the dependencies resolvable? | Yes — `IProductCatalogService` is registered as a singleton instance in `CatalogRegistrationExtentions`, and `WorkspaceSlotSettings` as an instance in `WorkspaceRegistrationExtentions` (via `ReadSlotSettings`, which is why a grep for the type finds no registration line) |
| File budget (≤ 150 code lines) | 108 + ~15 → **123** |
| Method budget (≤ 40 raw lines) | `Suggest` is unchanged at **37**; the two new methods are single expressions |

## Decision

**D1 — `SuggestionRequestBuilder` is deleted and its two methods are the controller's.** `Build` and `EverySlot`
are private methods of `BuilderController`, and `Build`'s body is unchanged: the same run id, the same trimmed
query, the same currency lookup, the same ceiling, the same slot rules, the same token.

**D2 — The controller takes `IProductCatalogService` and `WorkspaceSlotSettings` directly.** The catalogue is
named `everyProduct`, which is what `CatalogController` already calls the same dependency, so the two endpoints
name one service one way. `WorkspaceSlotSettings` is still read once at start-up — the merge moved where it is
*consumed*, not when it is *read*, so retuning a slot is still a configuration change rather than a rebuild.

**D3 — The class remark now says why a catalogue is a dependency of an endpoint that holds no catalogue.** It
would otherwise read as though this endpoint keeps the catalogue — the exact thing the agent-facing design
removed. The remark states it is there for the currency and for nothing else, and that the agent searches the
catalogue for itself through the MCP tools this application publishes.

**D4 — The payload reasoning moved onto `Build`**, where the payload is now built: the catalogue is not pushed
(~336 KB ≈ 86k tokens, twice, against a deployment that allows 100,000 tokens a minute); only the query, the
rules and the currency cross; and the token is left out of the payload hash because the hash identifies what the
run was drawn from rather than who asked.

**D5 — The registration is removed.** `SuggestionAgentRegistrationExtentions` now registers the settings, the
availability flag and the agent, and nothing that belongs to a run.

## Consequences

**Good**

- The AI feature has no per-run collaborator types left. What remains between the request and the model is the
  agent port and the controller.
- One fewer type, one fewer registration, and one fewer construction site: a reader following a run from the
  route to the wire crosses fewer files than before.
- Both budgets are comfortable: `BuilderController` **123 of 150** code lines, `Suggest` **37 of 40** raw lines,
  and the two new methods are expressions.

**Bad / recorded**

- **The endpoint now names a module contract** (`IProductCatalogService`) and a Workspace rule record
  (`WorkspaceSlotSettings`) to state a currency and a capacity table. Its constructor is four parameters, of
  which two exist for one value and one lookup. That is the cost of there being no builder.
- **The payload has no unit-level seam.** It never had a test of its own, and now there is nowhere to put one:
  the only way to exercise what crosses to Foundry is over HTTP. [0009](0009-the-run-is-the-endpoint.md) recorded
  this for the run, and this extends it to the payload.
- **Nothing here is verified at the browser tier**, which is red at HEAD for the reason
  [0006](0006-the-run-endpoint-is-a-controller.md) D6 gives.

## Alternatives rejected

| Alternative | Why rejected |
|---|---|
| Keep `SuggestionRequestBuilder` registered and injected | It is the thing the ask removes, and it exists for one operation |
| Inline `Build`'s body into the action instead of a private method | Measured: `Suggest` is 37 raw lines and the body is ~10 more — the method budget is 40 |
| Have `Build` take the trimmed query and the ceiling as parameters rather than the bound body | Splits one wire shape across three arguments to save nothing, and loses the `ArgumentNullException` guard on the thing being converted |
| Read the currency from the agent instead of the catalogue | The currency is what the request is *stated* in, so it has to be the application's before the request is sent |

## Baselines

Measured on 2026-09-25, in the same working tree as
[0006](0006-the-run-endpoint-is-a-controller.md)–[0010](0010-the-run-is-one-method.md).

| Baseline | Before this change | After |
|---|---|---|
| `BUILD` | `dotnet build CoreRentalNet.sln` — **0 warnings, 0 errors** | **0 warnings, 0 errors** |
| `NONBROWSER` | **713 passed, 0 failed** | **713 passed, 0 failed** (no test added or removed) |
| `BROWSER` | **red at HEAD**: 118 failed, 1 passed, 1 skipped — `Not built: …CoreRentalNet.E2E.LocalAgent.dll` | **unchanged**, same cause |
| `AGENT` | not run; nothing under `agentfoundry/` is touched | **40 passed, 0 failed** |

## Notes

- **A grep for `WorkspaceSlotSettings` finds no registration**, which reads like a missing dependency and is
  not: `WorkspaceRegistrationExtentions` registers an evaluated instance
  (`builder.Services.AddSingleton(ReadSlotSettings(configuration))`), so the type name appears only inside that
  private method. Anyone checking whether this dependency is resolvable should follow the method, not the type.
- **The class remark in [0006](0006-the-run-endpoint-is-a-controller.md) about why the controller is internal
  narrowed again.** It listed "the agent and the request builder"; only the agent is internal now, and the
  remark was rewritten to say so rather than keep a reason that had stopped being true.
