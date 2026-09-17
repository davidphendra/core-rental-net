# e04s05 — the destructive step, module half

**Date:** 2026-09-16
**Branch:** `e03-catalogue-contract`
**Status:** story **done** — the module, the Host mapper, the confirmation dialog and AIB-18 … AIB-21.

## What was run

| Check | Before | After |
|---|---|---|
| BUILD `CoreRentalNet.sln` | 0 warnings, 0 errors | **0 warnings, 0 errors** |
| NONBROWSER | 626 passed, 0 failed | **633 passed, 0 failed** |
| BROWSER | 113 passed, 1 skipped | **116 passed, 0 failed, 1 skipped** |
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

## The apply, in the Host

`CompositionFromOption` turns the chosen candidate into the composition the workspace is replaced with.
What does **not** cross is everything the agent said about *why* — the criteria, the tier's name, the
findings — because the Workspace module has no use for them.

**The catalogue is checked again here**, not trusted from the run. The two are minutes apart and a product
can leave the catalogue in between; a line the catalogue no longer holds is dropped and named. Checked
*before* the dialog appears rather than after it is confirmed, because a dropped line is part of what the
customer is agreeing to — telling them afterwards would make the confirmation a formality.

| Test | What it holds |
|---|---|
| AIB-18 | a non-empty workspace asks; the dialog names the count and the tier; **cancelling leaves the workspace as it was** |
| AIB-19 | confirming replaces the slots — three, not five — and the **delivery address survives**, proved by setting one, applying over it, and going to look |
| AIB-20 | an empty workspace applies with **no dialog**, the composition is the workspace, and the candidates go with it |

## Two collisions worth knowing about

1. **`DraftToken` is already a domain type.** The component's parameter could not be called that, because
   every page imports the module's domain. It is `Token`.
2. **`outcome` was shadowed.** A local `var outcome` in the apply path hid the field the page renders, so
   the line clearing it could never have worked. Renamed to `applied`.

## The design-token test reads *every word in a quoted literal*

My copy contained the word "left" — an ordinary English word, and also a Tailwind class in the design
prototype. The checker is deliberately crude: it extracts words from quoted literals and asks whether the
stylesheet generates them. Rewording the copy is cheaper than a cleverer test that would then miss real
ones. It caught me twice, the second time inside the **comment explaining the first** — the explanation
quoted the offending word.

## Not done, deliberately

- **Merging** a candidate into an existing workspace. Refused by design: a tier means desk, chair and
  monitor all chosen together, so a high-tier chair in a low-tier desk is not one of the three, and the
  label on the option the customer accepted would be a lie.
- **Undo.** The confirmation is the safeguard.
- **A browser test for the version conflict.** AIB-21 covers the rule at unit level and the page renders
  the message; driving two tabs through the browser suite is worth its own story rather than a paragraph
  of this one.
