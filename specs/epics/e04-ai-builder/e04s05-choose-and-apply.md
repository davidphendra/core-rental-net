# e04s05 — Choosing a candidate replaces the workspace

**type:** feat
**risk:** P0
**context:** application
**bcps:** 5
**status:** done

## Context

This is the destructive step, and the decisions that make it safe are already recorded: **replace the
slots, preserve the delivery address, confirm only when there is something to lose**, and apply the
whole composition in **one atomic write**.

The reason a **merge** is refused: a candidate means *low*, *middle* or *high* **as a whole** — desk,
chair, monitor, lamp and plant were all chosen at the same tier. Merging a high-tier chair into an
existing low-tier desk produces a workspace that is **none of the three**, so the label on the option
the customer accepted would be a lie, and the reviewer approved a composition nobody receives.

The reason it is **one command**: composing `RemoveAssignment` and `AssignProduct` would bump
`Version` once per line, so another tab would see a cascade of half-applied workspaces, and a failure
midway would leave a state that is neither the old workspace nor the candidate.

## Requirements

#### ADDED: `ApplyWorkspaceComposition`

One Application operation taking a plain composition (`slot`, `sku`, `quantity`) — **not** the agent's
candidate, whose rationale and criteria are not the Workspace module's business. It clears the slots
and assigns the composition in one transaction with **one `Version` bump**, and it does not touch the
delivery address because the address is not in its payload.

#### ADDED: confirmation, and a loud conflict

The page confirms only when the workspace is **non-empty**. If the draft changed between the run and
the apply — another tab, a long run — the write is **rejected** and the page says the workspace changed
and must be reviewed again. Last-write-wins would be exactly the silent lost update the repository
warns about.

#### ADDED: drop and report

A SKU the catalogue no longer holds is dropped and the customer is told which. A partial composition is
never applied silently.

## Zoom-Out

- **Module purpose:** the Workspace module owns the draft — its slots, quantities, address and quote.
  This operation replaces the slots and nothing else.
- **Callers:** the builder page, on the customer's confirmation.
- **Contracts to preserve:** the draft's existing operations stay as they are; prices are recomputed
  from the catalogue exactly as they are for a hand-built draft; the address is untouched.

## Steps

1. Add the composition contract to the Workspace application. → verify: `dotnet build CoreRentalNet.sln -v q --nologo`
2. Add `ApplyWorkspaceComposition` with its handler: clear the slots, assign the composition, bump
   `Version` once. → verify: `dotnet test tests/CoreRentalNet.Modules.Workspace.UnitTests --nologo --filter "FullyQualifiedName~ApplyComposition"`
3. Reject a stale version and report it. → verify: `dotnet test tests/CoreRentalNet.Modules.Workspace.UnitTests --nologo --filter "FullyQualifiedName~VersionConflict"`
4. Add the confirmation dialog, shown only when the workspace is non-empty. → verify: `dotnet build CoreRentalNet.sln -v q --nologo`
5. Map the chosen candidate to a composition in the Host, dropping unknown SKUs and reporting the
   drops. → verify: `dotnet build CoreRentalNet.sln -v q --nologo`
6. Unit test AIB-21 and browser tests AIB-18 … AIB-20. → verify: `dotnet test tests/CoreRentalNet.E2E --nologo --filter "FullyQualifiedName~AiBuilderApplyTests"`
7. Re-run the three baselines. → verify: `dotnet test CoreRentalNet.sln --filter "FullyQualifiedName!~CoreRentalNet.E2E" --nologo`

## Verification Script (Step-by-Step)

1. With a non-empty workspace, select a candidate → a confirmation naming what will be replaced.
2. Confirm → the slots hold exactly the composition, and the delivery address is unchanged.
3. With an empty workspace, select a candidate → no dialog, applied directly.
4. Cancel the dialog → nothing changes.
5. Edit the draft in a second tab, then confirm in the first → the apply is refused and the page says
   the workspace changed.
6. Select a candidate after removing a product from the catalogue → the line is dropped and reported.

## Test matrix

| ID | Scenario | Level |
|----|----------|-------|
| AIB-18 | A non-empty workspace asks before replacing | browser |
| AIB-19 | Confirming replaces the slots and leaves the address | browser |
| AIB-20 | An empty workspace applies without a dialog | browser |
| AIB-21 | A version conflict applies nothing and says so | unit |

## Out of scope

- Merging a candidate into an existing workspace. Refused by design; protecting existing work is what
  a draft-aware suggestion would be for, and it is deferred.
- Undo. The confirmation is the safeguard.
- Any change to checkout, invoicing or the order model.

## Risks

- **Losing work without saying so.** The dialog names what will be replaced, and it appears only when
  there is something to lose.
- **A partial apply.** The single operation is what prevents it; composing existing commands would
  reintroduce it.
- **A silent lost update.** The version check fails loudly, which is the repository's stated rule.
- **Dropping a line silently.** Every drop is reported.

## Acceptance criteria

- AIB-18 … AIB-21 pass.
- After confirming, the slots hold exactly the composition and the address is unchanged.
- A version conflict applies nothing and is visible to the customer.
- The three baselines are green.
