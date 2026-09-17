# e04s03 — The customer sees the run happen, and can stop it

**type:** feat
**risk:** P0
**context:** ui
**bcps:** 5
**status:** in-progress

## Context

A run is up to **ten model calls across three attempts**, so it can be long. Without visible progress
the customer cannot tell a slow workflow from a stuck one, and without a cancel the only escape is a
reload. The agent emits stage **ids**; the application owns the **words**, so renaming an agent's stage
never changes what a customer reads.

## Requirements

#### ADDED: the AI section and its streamed run

The builder gains a permission-gated section holding a field for the request. On submit the
application calls the port and renders a **stage list** as the run proceeds — *"Checking your request →
Understanding what you need → Choosing products → Reviewing the options"*, with *"attempt 2 of 3"*
where it applies. A stage id the application does not know renders nothing rather than leaking.

The stage list is announced with `aria-live="polite"` and **never** assertively, so a screen reader is
not interrupted on every transition. The list is **retained after the run and collapsed**, so the
answer to "why did that take so long" survives, and the results take visual focus.

#### ADDED: a cancellable run

A cancel control aborts the stream and **applies nothing** — nothing is applied until a candidate is
chosen, so cancelling is safe by construction. Cancelling releases the run guard (`e04s06`).

## Zoom-Out

- **Module purpose:** the builder page composes a draft on a fixed canvas. This section proposes
  content for it and never writes to it.
- **Callers:** the customer, and the browser suite.
- **Contracts to preserve:** the page's existing layout, its `SelectionPanel`, `SlotCanvas`, `ZoneList`,
  `CategoryChips` and `MonthlyTotalBar`; the design prototype at
  `specs/design/interactive_builder/code.html`; the mobile arrangement at `lg` and below.

## Steps

1. Add the AI section component, hidden unless the entitlement holds. **Not started**: it needs the
   Host adapter to call, which is the next step. → verify: `dotnet build CoreRentalNet.sln -v q --nologo`
2. Add the request field and the submit path calling the port. → verify: `dotnet build CoreRentalNet.sln -v q --nologo`
3. Add the stage-id to copy map, owned by the Host, with an unknown id rendering nothing. → verify: `dotnet test tests/CoreRentalNet.Host.Tests --nologo --filter "FullyQualifiedName~StageCopyTests"`
4. Render the streamed stages with `aria-live="polite"`, and retain and collapse the list when the run
   ends. → verify: `dotnet build CoreRentalNet.sln -v q --nologo`
5. Add cancel, aborting the stream and applying nothing. → verify: `dotnet build CoreRentalNet.sln -v q --nologo`
6. Browser tests AIB-11 … AIB-13 against the local stand-in. → verify: `dotnet test tests/CoreRentalNet.E2E --nologo --filter "FullyQualifiedName~AiBuilderStreamTests"`
7. Re-run the three baselines. → verify: `dotnet test CoreRentalNet.sln --filter "FullyQualifiedName!~CoreRentalNet.E2E" --nologo`

## Verification Script (Step-by-Step)

1. With the stand-in in its slow scenario, submit a request → stages appear in order.
2. Confirm the list is announced politely, and the page is not scrolled or focused away.
3. Confirm a stage id the application does not know renders nothing.
4. Cancel mid-run → no candidates appear, and the workspace is untouched.
5. Complete a run → the list collapses and remains available.
6. Check the section at the mobile breakpoint, where the docked panel is absent.

## Test matrix

| ID | Scenario | Level |
|----|----------|-------|
| AIB-11 | A submitted request shows stages, then candidates | browser |
| AIB-12 | The stage list is retained after the run and collapsed | browser |
| AIB-13 | Cancelling a run applies nothing | browser |

## Out of scope

- What each outcome says (`e04s04`).
- Applying a candidate (`e04s05`) and the run guard (`e04s06`).
- Streaming model tokens: the answer is a structured object, so partial output is not renderable.

## Risks

- **The agent's stage vocabulary becoming customer copy.** Ids cross the boundary; the words live in
  one map in the Host.
- **An assertive announcement interrupting a screen reader.** Polite only, asserted.
- **A cancelled run leaving state behind.** Cancel must abort and clear, and `e04s06` must see the
  guard released.
- **The section breaking the canvas layout.** The prototype is the reference, and the existing browser
  tests for the canvas must keep passing.

## Acceptance criteria

- AIB-11 … AIB-13 pass against the local stand-in, with no network.
- The stage list is polite, retained and collapsed; an unknown stage id renders nothing.
- Cancelling applies nothing and the builder is otherwise unchanged.
- The three baselines are green.
