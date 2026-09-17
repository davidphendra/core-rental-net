# e04s03 — the section, the run, and the cancel

**Date:** 2026-09-16
**Branch:** `e03-catalogue-contract`

## What was run

| Check | Before | After |
|---|---|---|
| BUILD `CoreRentalNet.sln` | 0 warnings, 0 errors | **0 warnings, 0 errors** |
| NONBROWSER | 601 passed, 0 failed | **601 passed, 0 failed** |
| BROWSER | 104 passed, 1 skipped | **108 passed, 0 failed, 1 skipped** |
| `agentfoundry/AgentFoundry.sln` | 38 passed | **38 passed** |

## What the page does

`Components/Shared/AiBuilderSection.razor`, in the builder's own column beside the canvas, the zones and
the category chips. Gated by `AuthorizeView` on `AiBuilderReadPolicy` — its own permission, not the
page's, so a customer without the entitlement sees the builder **unchanged**.

| Requirement | How it is met |
|---|---|
| a field and a submit | `@onsubmit` with `preventDefault`; the submit is disabled while a run is happening |
| stages as the run proceeds | each `SuggestionUpdate` with a stage id appends a line |
| in the application's words | `SuggestionStageCopy.For` — the agent sends ids, the Host owns the sentences |
| "attempt 2 of 3" | `SuggestionStageCopy.Attempt`, appended where the attempt is past the first |
| an unknown id renders nothing | `For` returns null and the line is not emitted |
| retained and collapsed after the run | the list stays, and only a "What took so long?" control shows |
| cancel applies nothing | nothing is applied until a candidate is chosen, so cancelling **is** safe by construction |
| the canvas is untouched | the section never writes to the workspace; it holds an outcome and no more |

## The defect the page had, and what the fix was

**The live region was created at the same moment as its content.** `aria-live="polite"` sat on the
`<ol>`, and the `<ol>` appeared in the same render as the first stage — which is the one arrangement
that reliably announces nothing: a browser has to be *observing* the element before it changes, and an
element that arrives already full never changes at all.

My first comment on it claimed the opposite and read as a justification, which is how a bug becomes a
documented decision. It is now a permanent, visually hidden region that outlives the runs:

```
<p class="sr-only" data-testid="ai-announcement" aria-live="polite">@announcement</p>
```

The attempt is part of the announced sentence on purpose: two attempts at the same stage would otherwise
produce identical text, and a live region whose text does not change is not announced at all — so the
second attempt would pass in silence while appearing correct in the DOM.

AIB-11 now asserts the region is **attached and empty before the run**, and that it moves during it.
Against the old markup that test could not have been written.

## A run that fails is not a run that was cancelled

`SuggestWorkspaceOptions.NextAsync` treated every `TaskCanceledException` as an unreachable agent. A
timeout and a customer pressing Cancel arrive as the same exception and mean opposite things, so a
cancelled run reported itself as an **unavailable service** — blaming the agent for our own decision.
The exception is now passed on when *our* token was cancelled, and only an unreachable agent becomes
that answer.

## A slow scenario, because two of these tests are about time

`ScenarioLibrary` gained `slow`: the same answer as the happy path, delivered 900 ms per event. Without
it the tests that matter most would have been races rather than checks — a stage list collapsed before
it could be read, and a cancel button with nothing left to interrupt. The first attempt at 400 ms
failed exactly that way, and the count passing through 4 on its way to 0 is what the failure printed.

## The tests

| Test | What it holds |
|---|---|
| `AIB_11` | the region is present and empty, moves politely, four stages in order, then the candidates |
| `AIB_12` | the list survives the run, collapsed, `aria-expanded="false"`, and reopens with all four |
| `AIB_13` | cancel ends the run, shows no candidate, and leaves the canvas with nothing filled |
| a closed section | an account without the entitlement gets **no** section — absent, not empty |

## Measured, not asserted

`data-interactive` and the existing `.workspace-stage` marker are what the sign-in waits on, and the
canvas count is asserted **before** the run as well as after, so AIB-13 proves the workspace was
untouched rather than that nothing was added twice.

## Not done, deliberately

- **What each outcome says** — `e04s04`, together with the one-candidate rule for a non-power-user.
- **Applying a candidate** — `e04s05`, and the run guard in `e04s06`.
- **Streaming model tokens** — the answer is a structured object, so partial output is not renderable.
