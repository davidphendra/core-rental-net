# e04s03 — the streaming shape and the words (partial)

**Date:** 2026-09-16
**Branch:** `e03-catalogue-contract`

## What was run

| Check | Before | After |
|---|---|---|
| BUILD `CoreRentalNet.sln` | 0 warnings, 0 errors | **0 warnings, 0 errors** |
| NONBROWSER | 577 passed, 0 failed | **585 passed, 0 failed** |
| BROWSER | 104 passed, 1 skipped | **unchanged** |
| `agentfoundry/AgentFoundry.sln` | 38 passed | **unchanged** |

## What changed

- **`SuggestionUpdate`** and **`AgentSuggestionMessage`** — the streaming contract on both sides of the
  port, discriminated by `kind` because the agent's own wire contract is.
- **`ISuggestWorkspaceOptions.SuggestAsync`** now returns `IAsyncEnumerable<SuggestionUpdate>`, and
  **`IAgentSuggestions.AskAsync`** returns a stream: a run is several model calls and a page that
  waited for the checking before showing anything would have nothing to show while a customer waits.
- **`SuggestWorkspaceOptions`** streams stages through as they arrive and applies the rules to the
  answer — the same four rules as before, now on the last message of a stream.
- **`SuggestionStageCopy`** — the application's words for the agent's stage ids, in the Host.

## Measured effect

| | |
|---|---|
| A run's updates | `stage`, `result` — in that order, asserted |
| A stage that reaches the page | the **application's words**, never the agent's id |
| An id this application does not know | **renders nothing** |
| A stream that fails while being read | the same `unavailable` a request that never connected produces |

## What the run corrected

1. **`SuggestAsync` breached the nesting budget at 4 levels.** `await using` → `while` → `try` → `if`,
   against a limit of three, caught by `CodingStandardTests`. One step of the enumeration is now its own
   method, which also reads better: a stream fails *while it is read*, and the try belonged around that
   rather than around the call that created it.
2. **The same method had already breached the length budget at 49 lines** in the previous story, and the
   fix then — separating asking from mapping — is what made this one a single extra level rather than
   two.

## Not done — and the dependency is real, not a choice

- **The section itself is not built.** No Razor component, no field, no cancel, and `AIB-12` and `AIB-13`
  are not written.
- **The local stand-in process is not built**, and neither is the Host adapter.

These three are one chain, and the first two cannot be verified before it is closed:

```
the page  →  the Host adapter  →  the local stand-in
```

The page needs something to call. The adapter is that something, and the stand-in is what the adapter
talks to. Building the page first would produce a section that can only ever say `unavailable`, with no
way to assert that it streams, collapses or cancels — three of the things this story exists for.

**The next step is therefore the adapter**, written against the contract rather than against Foundry: a
`HttpClient` and the two record types that already exist, tested against a hand-written message handler
with no network. The stand-in process follows it, and the page follows that. What `e02s07` adds at the
end is the credential and the Responses-protocol mapping — not the shape.

Recorded here rather than left implicit, because a story marked done with three of its five acceptance
criteria unmet is worse than one marked partially done with the reason written down.
