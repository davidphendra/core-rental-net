# e04s02 — the application can ask the agent and understand the answer

**Date:** 2026-09-16
**Branch:** `e03-catalogue-contract`

## What was run

| Check | Before | After |
|---|---|---|
| BUILD `CoreRentalNet.sln` | 0 warnings, 0 errors | **0 warnings, 0 errors** |
| NONBROWSER | 572 passed, 0 failed | **577 passed, 0 failed** |
| BROWSER | 104 passed, 1 skipped | **unchanged** |
| `agentfoundry/AgentFoundry.sln` | 38 passed | **unchanged** |

## What changed

- **`Contracts/Suggestion/`** — the request, the outcome, an option and a line; and the four statuses,
  `ok`, `exhausted`, `rejected` and the application's own `unavailable`.
- **`Contracts/Suggestion/AgentSuggestion*`** — what the agent says, kept deliberately raw: unchecked
  SKUs and criteria tokens, so that what the agent claimed and what the application will show are two
  different things and the checking in between is visible.
- **`IAgentSuggestions`** — the port, with `IsConfigured` so that a deployment with no agent is never
  asked to reach one.
- **`ISuggestWorkspaceOptions` / `SuggestWorkspaceOptions`** — the rules.

## Measured effect

| Case | Result |
|---|---|
| A candidate | priced and named **from the catalogue**, never from the agent |
| A SKU the catalogue does not hold | **dropped and reported** in `DroppedSkus` |
| An option whose lines all drop | **not shown** — one candidate, not three and not two |
| No agent configured | `unavailable`, and the agent is **never asked** |
| An agent that cannot be reached | `unavailable`, distinct from `rejected` |

## What the run corrected

1. **The service's own method breached the coding budget.** `SuggestAsync` came to 49 lines against a
   40-line limit, and `CodingStandardTests` caught it — the architecture test doing exactly what it
   exists for. Asking was split from mapping, which also made the two failures it handles - unreachable
   and refused - read as two things rather than one branch inside a longer method.
2. **Three files landed in the repository root.** Two shell commands issued together raced, so a `cd`
   into a directory the other command had not yet created failed and three files were written at the
   root. Caught by the next `ls`, removed, and rewritten in place. Worth recording because a stray file
   at the root is the kind of thing that survives a review.

## Not done, deliberately

- **The Host adapter is not written.** `IAgentSuggestions` has no implementation yet, because the
  transport it would talk to does not exist: the hosted agent's endpoint arrives with `e02s07`, together
  with the credential `C-1` names. Writing an HTTP client against an endpoint nobody serves would be
  unverifiable code.
- **The local stand-in process is not built.** `tests/CoreRentalNet.E2E.LocalAgent` is the browser
  tier's agent, and the browser tier has nothing to drive it with until the page exists (`e04s03`).
  Building a scenario-selectable server before its consumer means inventing the scenarios.
- **The browser-suite half of the acceptance criteria** — "the suite can run the whole feature against
  the stand-in" — therefore moves with both.

Everything the story can deliver without a transport or a page is delivered and asserted; the two
missing pieces have one consumer each, and neither can be tested before it exists.
