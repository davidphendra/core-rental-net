# e02s04 — The catalogue becomes three candidates

**type:** feat
**risk:** P0
**context:** infra
**bcps:** 8
**status:** passing

## Context

This is the only component that reads the catalogue (AGENT-4). It reads it **once per request** and
reuses the snapshot across attempts, because the catalogue is an immutable start-up snapshot and
attempts two and three would fetch identical data (RET-4, RET-5).

The tier rule is the whole product: **per slot, sorted by price, low = cheapest, middle = median
index, high = most expensive, ties by SKU** (P1). It is exact and reproducible, which is why the
candidate set must be **complete** — a relevance-ranked or truncated set would move the median, and
`e03s03` is what lets this story detect it.

## Requirements

#### ADDED: one catalogue read, and the three candidates

The suggestor reads `GET /api/catalog?view=compact` once, groups products by the specification's
slots, and applies P1 within each slot to produce three candidates across the fixed slot set. It
issues **one** authenticated read per request, not one per attempt.

A slot with **fewer than three candidates is pinned** — the same product in every candidate — and
listed in `pinnedSlots`. When **every** slot is pinned the result carries **one** candidate, not three
identical ones.

A slot whose candidate set is **truncated** is refused rather than tiered, since its median depends on
how many rows arrived (the rule `e03s03` creates).

#### ADDED: criteria mapped, or disclosed

Each criterion from the specification is resolved against the **metadata vocabulary** —
`tag:<token>`, `attribute:<SlotId>:<key>:<value>` — or, when nothing matches, recorded in
**`unevaluated[]`** with the user's own phrase and a reason. Every option carries both lists, so a
criterion is either met or visibly unmet and never silently dropped (SAFE-1/C3).

## Zoom-Out

- **Module purpose:** select products and compose candidates. It states no prices — the application
  recomputes them — and it does not judge its own work.
- **Callers:** the reviewer, and the application over the contract.
- **Contracts to preserve:** the contract schemas from `e02s02`; `SlotId`; the rule that an option
  names SKUs and quantities only.

## Steps

1. Add the catalogue reader port and the shapes it answers with. The HTTP client behind it, and the
   credential it carries, arrive with the deployment (`e02s07`) - the path is decided by `e02s01`.
   → verify: `dotnet build agentfoundry/AgentFoundry.sln -v q --nologo`
2. Group products by slot and implement P1 (ties by SKU) as one function. → verify: `dotnet test agentfoundry/tests/AgentFoundry.Tests --nologo --filter "FullyQualifiedName~TierTests"`
3. Add pinning and the disclosure, and the single-candidate case. → verify: `dotnet test agentfoundry/tests/AgentFoundry.Tests --nologo --filter "FullyQualifiedName~PinningTests"`
4. Refuse to tier a truncated slot, reading `total` and `truncated` from the envelope. → verify: `dotnet test agentfoundry/tests/AgentFoundry.Tests --nologo --filter "FullyQualifiedName~TruncationTests"`
5. Implement criteria resolution against the metadata vocabulary, with `unevaluated[]` for the rest.
   → verify: `dotnet test agentfoundry/tests/AgentFoundry.Tests --nologo --filter "FullyQualifiedName~CriteriaTests"`
6. Assert one catalogue read for the whole run. → verify: `dotnet test agentfoundry/tests/AgentFoundry.Tests --nologo --filter "FullyQualifiedName~CatalogueCallCountTests"`
7. Unit tests AGT-08 … AGT-13. → verify: `dotnet test agentfoundry/tests/AgentFoundry.Tests --nologo`

## Verification Script (Step-by-Step)

1. Run the container against the real catalogue with a scripted model.
2. Send a request for a monitor and a chair → three candidates whose monitor SKUs are the cheapest,
   the median-index and the most expensive of the slot, and whose chair SKUs differ likewise.
3. Confirm the slot sets of the three candidates are identical.
4. Ask for something the catalogue cannot express (*"reliable"*) → the criterion appears in
   `unevaluated[]` on every candidate with the user's own phrase.
5. Ask for a phrase the metadata holds (*"standing"*) → the criterion resolves to an attribute and
   the selected SKUs change accordingly.
6. Force a truncated slot and confirm the run refuses to tier it rather than guessing.

## Test matrix

| ID | Scenario | Level |
|----|----------|-------|
| AGT-08 | Cheapest, median index, most expensive per slot, ties by SKU | unit |
| AGT-09 | A slot with fewer than three candidates is pinned and disclosed | unit |
| AGT-10 | All slots pinned yields one candidate | unit |
| AGT-11 | A criterion resolves to `tag:`/`attribute:` or appears in `unevaluated[]` | unit |
| AGT-12 | The catalogue is read once for the whole run | unit |
| AGT-13 | A truncated slot is refused rather than tiered | unit |

## Out of scope

- The retry loop and the reviewer (`e02s05`).
- Prices, names and images: the application resolves them.
- Mixing products inside one slot.

## Risks

- **Tiering whatever the retriever returned.** The reason the rule is price-position and the read is
  unfiltered; AGT-08 and AGT-13 are the guards.
- **A criterion silently dropped.** Only `criteria` and `unevaluated` exist, so there is nowhere for
  one to disappear — but the mapping must be exhaustive, and AGT-11 asserts it.
- **The catalogue read repeated per attempt.** Costs an authenticated round trip each time; AGT-12
  asserts one read for the run, and the across-attempts half of that arrives with the retry loop.
- **A failing node looking like a short run.** Microsoft Agent Framework ends the stream when a node
  throws, so an unread `WorkflowErrorEvent` makes a failure indistinguishable from a request that
  simply produced no result. The run reads it and rethrows.

## Acceptance criteria

- AGT-08 … AGT-13 pass with a fake catalogue client and no network.
- Every option carries a disposition for every criterion.
- The application's three baselines are untouched.
