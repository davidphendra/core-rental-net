# e04s05 — the destructive step, module half

**Date:** 2026-09-16
**Branch:** `e03-catalogue-contract`
**Status:** story **in-progress** — steps 1–3 and 6's unit half are done; the Host mapper, the
confirmation dialog and AIB-18 … AIB-20 are next.

## What was run

| Check | Before | After |
|---|---|---|
| BUILD `CoreRentalNet.sln` | 0 warnings, 0 errors | **0 warnings, 0 errors** |
| NONBROWSER | 626 passed, 0 failed | **633 passed, 0 failed** |
| BROWSER | 113 passed, 1 skipped | **113 passed, 0 failed, 1 skipped** |
| `agentfoundry/AgentFoundry.sln` | 38 passed | **38 passed** |

## What was built

`ApplyCompositionCommand(DraftToken, ExpectedVersion, Lines)` and its handler, plus
`IWorkspaceService.Replace` and `CompositionWriter`.

| Decision | How it is made impossible rather than remembered |
|---|---|
| one atomic write | one `Replace`, one `Touch`; three lines bump the version **once** |
| the address is preserved | it is **not in the payload** — nothing to restore because nothing can change |
| no silent lost update | `ExpectedVersion` is compared before anything is written; a mismatch returns `Stale` |
| no half-applied workspace | every line is built **before** anything is cleared |
| the label on the option stays true | a composition naming a slot twice is refused — merging a high-tier chair into a low-tier desk is not one of the three tiers |

**The bug I wrote and caught.** My first `Replace` cleared the assignments and *then* built each line, so a
rejected line left a workspace that was neither what it was nor what was asked for — the exact half-applied
state the operation exists to prevent, in the operation itself. `A_composition_with_a_line_that_cannot_be_written_changes_nothing`
is the test that caught it, and building the whole list first is the fix.

## The fixture was wrong again, in the same way

The contract's line `slot` is a closed list of **PascalCase** names that match `SlotId` exactly. My
stand-in said `"desk"`. Nothing consumed the value — `SuggestedLine.Slot` was carried and never used — so
**nothing noticed**, and it would have surfaced at the last moment as a refusal from the workspace after
the whole pipeline had accepted it.

The fixture now says `Desk`, and `LocalAgentFixtureTests` holds the line's slot to the contract's enum
alongside the tier, criteria, status and finding vocabularies it already checked. This is the second time
a fixture has been found speaking a language the contract forbids, and both times every test passed.

## The tests

| Test | What it holds |
|---|---|
| AIB-21 | a stale version applies **nothing** — not a smaller write, not a corrected one — and the desk already there survives |
| `The_composition_is_the_workspace_afterwards` | replaced, not added to: the old assignment is gone |
| `Three_lines_bump_the_version_once` | the reason the operation exists rather than being composed from the existing commands |
| `The_delivery_address_is_left_alone` | the address survives an apply |
| `A_composition_with_a_line_that_cannot_be_written_changes_nothing` | all-or-nothing |
| `A_slot_named_twice_is_refused` | a composition that is not one answer |
| `A_workspace_that_became_an_order_can_no_longer_be_replaced` | the existing rule still applies |

## What needed splitting, and why

Two budget failures, both from adding to something already near the line:

1. **`WorkspaceService.cs` reached 280 lines.** The composition rules moved to
   `Commands/ApplyComposition/CompositionWriter.cs`, which is where they belong anyway: the service can
   assign one product to one slot, and knowing what a *whole* composition has to be is a different job.
2. Earlier in the story, `ResponsesAgentTextStream.AskAsync` (43 lines) was split so the translation sits
   outside the loop that yields — a `yield return` cannot live in a try with a catch.

## Not done, deliberately

- **The Host mapper** (step 5): candidate → composition, dropping SKUs the catalogue no longer holds and
  reporting the drops, re-checked at apply time because a product can leave the catalogue between the run
  and the apply.
- **The confirmation dialog** (step 4), shown only when the workspace is non-empty, naming what will be
  replaced.
- **AIB-18 … AIB-20** (browser).
