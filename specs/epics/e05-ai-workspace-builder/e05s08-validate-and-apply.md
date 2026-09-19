# e05s08 — Nothing is shown or applied that was not checked

**type:** feat
**risk:** P0
**context:** app
**bcps:** 8
**status:** planned

## Context

The agent names SKUs; the application owns truth. Between them sits the check that makes the design
safe. It is **all-or-nothing**: one bad line fails the run rather than being quietly dropped, because a
partially-honoured suggestion is a lie about the price.

## Requirements

#### ADDED: validation before display

Validation runs on the whole result before a single candidate is rendered. Every SKU exists, its slot is
allowed, **`quantity ≥ 1`**, and `quantity ≤ capacity`. Prices, names and totals are resolved from the
catalogue here, never taken from the agent.

The lower bound is **not** redundant. `e05s01` established that the agent framework's deserializer accepts
a zero quantity silently — the schema forbids it and nothing on the agent's path enforces it — so this is
the only rule that rejects a line nobody can rent.

**Not validated, by decision:** there are no budget bands. Labels are relative, so there is no band an
option could contradict.

#### ADDED: the spread rule

The dearest valid option must be at least **1.5×** the cheapest, with that factor read from
**configuration** so the evaluation tier can tune it without a rebuild. The rule applies to the
**range only**: the middle option is merely sorted between them and carries no second margin, because a
middle margin can only ever refuse an option the range rule already accepted. Where the catalogue cannot
produce options that far apart, **fewer are shown** — never a padded range, never a failed run.

#### ADDED: sorting and labelling

The application sorts valid options by monthly total and labels them Budget / Balanced / Premium by
**rank**. Three, two or one are each labelled correctly, and **fewer than three distinct options are
shown as fewer, never padded**.

#### ADDED: the apply command

`ReplaceComposition` — one atomic, versioned write that replaces the **composition** and leaves the
**delivery address and every other draft field untouched**. A stale write is refused loudly and applies
nothing.

#### ADDED: confirm-then-replace

Choosing a candidate on a non-empty workspace asks first, naming what will be discarded. An empty
workspace applies without a dialog.

The copy: **Replace your workspace?** · *"This replaces the N items you have already picked. Your
delivery address is not changed."* · **Keep mine** / **Replace**.

## Zoom-Out

- **Module purpose:** the trust boundary, and the point at which an agent's answer becomes an
  application fact.
- **Callers:** the streamed run and the builder page.
- **Contracts to preserve:** one atomic write per apply; the address is not part of the composition;
  nothing is applied on any failure.

## Steps

1. Validate a whole result all-or-nothing against the catalogue and the slot capacities, including
   `quantity ≥ 1` — which nothing upstream rejects, as `e05s01` established.
   → verify: `dotnet test tests/CoreRentalNet.Host.Tests --nologo --filter "FullyQualifiedName~SuggestionValidationTests"`
2. Sort and label by rank, and enforce the configured spread on the range; prove three, two and one are
   each labelled correctly and that too-close options yield fewer rather than padding.
   → verify: `dotnet test tests/CoreRentalNet.Host.Tests --nologo`
3. Add `ReplaceComposition` as one versioned command that touches the composition only.
   → verify: `dotnet test tests/CoreRentalNet.Modules.Workspace.UnitTests --nologo`
4. Add the confirm-then-replace path in the builder.
   → verify: `dotnet build CoreRentalNet.sln -v q --nologo`
5. Tests AIWB-31 to AIWB-38, unit and browser.
   → verify: `dotnet test tests/CoreRentalNet.E2E --nologo --filter "FullyQualifiedName~AiBuilderApplyTests"`
