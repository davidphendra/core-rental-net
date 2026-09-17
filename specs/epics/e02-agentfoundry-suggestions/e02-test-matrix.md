# e02 — Agentfoundry suggestions test matrix

Two levels only, because this epic lives beside the application rather than inside it: **unit** =
`agentfoundry/tests/AgentFoundry.Tests` with a hand-written fake catalogue client and a fake model, no
network; **live** = a test that reaches a real Foundry project, skipped unless credentials are in the
environment (the same opt-in pattern as `RealTenantTests`).

| ID | Story | Scenario | Level | Risk |
|----|-------|----------|-------|------|
| AGT-01 | e02s02 | A request that is not about a workspace returns `rejected` with a reason code and no options | unit | P0 |
| AGT-02 | e02s02 | Every message carries `requestId`, and the result carries `status` and `attempts` | unit | P0 |
| AGT-03 | e02s02 | Stage events arrive in order and the terminal event carries the result, exactly once | unit | P0 |
| AGT-04 | e02s03 | A phrase declared in the intent table produces its slot set | unit | P0 |
| AGT-05 | e02s03 | A table miss falls back to the closed `SlotId` enum and marks the set inferred; the miss is logged | unit | P1 |
| AGT-06 | e02s03 | Quantity defaults to 1 and is clamped to the capacity the request carries | unit | P1 |
| AGT-07 | e02s03 | The same request produces the same specification, and one run asks for it once | unit | P0 |
| AGT-08 | e02s04 | Price position: cheapest, median index, most expensive per slot, ties by SKU | unit | P0 |
| AGT-09 | e02s04 | A slot with fewer than three candidates is pinned in every candidate and disclosed | unit | P0 |
| AGT-10 | e02s04 | When every slot is pinned the result carries one candidate, not three identical ones | unit | P1 |
| AGT-11 | e02s04 | A criterion resolves to `tag:` or `attribute:`, or appears in `unevaluated[]` | unit | P0 |
| AGT-12 | e02s04 | One catalogue read per request, reused across attempts | unit | P0 |
| AGT-13 | e02s04 | A slot whose candidate set is truncated is refused rather than tiered | unit | P0 |
| AGT-14 | e02s05 | The validator rejects a wrong tier pick and repairs it in code, with no further model call | unit | P0 |
| AGT-15 | e02s05 | A semantic failure produces findings, re-enters the rephraser, and stops after three attempts | unit | P0 |
| AGT-16 | e02s05 | A retry specification equivalent to one already attempted is refused by the oscillation guard | unit | P1 |
| AGT-17 | e02s05 | Exhaustion returns the candidates with `exhausted` and the outstanding findings | unit | P0 |
| AGT-18 | e02s06 | One structured record per run carries outcome, attempts, catalogue calls and whether the slot set was inferred | unit | P1 |
| AGT-19 | e02s06 | Catalogue text cannot become an instruction: an injected string in a product field changes no decision | unit | P0 |
| AGT-20 | e02s07 | The deployed agent answers with exactly three schema-valid candidates | live | P0 |
| AGT-21 | e02s07 | Without `read:catalog` the agent's read is refused and no products arrive | live | P0 |
| AGT-22 | e02s07 | The agent's catalogue read appears once in the endpoint's structured log | live | P1 |
| AGT-23 | e02s07 | An unsatisfiable request yields no invented products | live | P0 |

## Baselines

| Baseline | Requirement |
|---|---|
| BUILD | `dotnet build CoreRentalNet.sln` **and** `dotnet build agentfoundry/AgentFoundry.sln`, both 0 warnings 0 errors |
| NONBROWSER | unchanged by this epic - no application code changes here, and 0 failed |
| BROWSER | unchanged by this epic, 0 failed |
| LIVE | AGT-20 … AGT-23 pass with credentials present and **skip cleanly** without them |

The application's three baselines are the point of the layout: nothing in `agentfoundry/` may move
them. A story in this epic that changes `CoreRentalNet.sln` is out of place and belongs in `e03` or
`e04`.

## Non-goals in this matrix

- No browser scenario: the agent has no UI. The page is `e04`.
- No load, rate-limit or cost scenario: one server-side caller, and the run is bounded at three
  attempts by design.
- No prompt-quality evaluation: the golden-query dataset is deferred and its first rows are captured
  by hand under `specs/verifications/`.
