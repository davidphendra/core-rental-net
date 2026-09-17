# e04 — AI builder test matrix

Levels: `unit` = `tests/CoreRentalNet.Host.Tests` with a hand-written fake agent client, no network;
`browser` = the Playwright suite against a real application process and the real local agent stand-in
(`tests/CoreRentalNet.E2E.LocalAgent`) — no mocks and no intercepted calls, as the suite requires.

| ID | Story | Scenario | Level | Risk |
|----|-------|----------|-------|------|
| AIB-01 | e04s01 | An account holding both permissions reaches the AI section and a power-user sees three candidates | unit | P0 |
| AIB-02 | e04s01 | An account holding `read:catalog` but not `read:aibuilder` sees no AI section | unit | P0 |
| AIB-03 | e04s01 | With no identity provider configured the AI section is **absent**, not open | unit | P0 |
| AIB-04 | e04s01 | `poweruser:aibuilder` alone grants nothing | unit | P1 |
| AIB-05 | e04s01 | Regression: the catalogue's own rule still opens when no provider is configured | unit | P0 |
| AIB-06 | e04s02 | A valid agent result is mapped to candidates with prices recomputed from the catalogue | unit | P0 |
| AIB-07 | e04s02 | A SKU the catalogue does not hold is dropped, and the drop is reported | unit | P0 |
| AIB-08 | e04s02 | Fewer than three valid candidates is stated as fewer, never padded | unit | P1 |
| AIB-09 | e04s02 | An unreachable agent yields a stated unavailability, distinct from a refusal | unit | P0 |
| AIB-10 | e04s02 | A refusal and a transport failure are different outcomes | unit | P0 |
| AIB-11 | e04s03 | A submitted request shows stages, then candidates | browser | P0 |
| AIB-12 | e04s03 | The stage list is retained after the run and collapsed | browser | P1 |
| AIB-13 | e04s03 | Cancelling a run applies nothing | browser | P0 |
| AIB-14 | e04s04 | A refusal shows the application's own message and no candidates | browser | P0 |
| AIB-15 | e04s04 | An exhausted run shows its candidates with the caveat | browser | P0 |
| AIB-16 | e04s04 | A non-power-user is shown one candidate, and it is the middle one | browser | P0 |
| AIB-17 | e04s04 | Criteria the catalogue could not express are shown on the candidate | browser | P1 |
| AIB-18 | e04s05 | Selecting a candidate with a non-empty workspace asks before replacing | browser | P0 |
| AIB-19 | e04s05 | Confirming replaces the slots and leaves the delivery address unchanged | browser | P0 |
| AIB-20 | e04s05 | An empty workspace applies without a dialog | browser | P1 |
| AIB-21 | e04s05 | A version conflict applies nothing and says the workspace changed | unit | P0 |
| AIB-22 | e04s06 | A second submit while a run is in flight is refused | unit | P0 |
| AIB-23 | e04s06 | The guard is released once the run ends, including when cancelled | browser | P1 |
| AIB-24 | e04s01 | A deployment with no identity provider shows no AI section anywhere on the page | browser | P0 |

## Baselines

Record the current numbers at kickoff: they have moved since `e01` was recorded.

| Baseline | Requirement |
|---|---|
| BUILD | `dotnet build CoreRentalNet.sln` and `dotnet build agentfoundry/AgentFoundry.sln`, both clean |
| NONBROWSER | previous total + the unit tests above, 0 failed, **and still hermetic** |
| BROWSER | previous total + the browser tests above, 0 failed, driven by the local agent stand-in |

**The hermetic property is the requirement, not a nice-to-have.** If any AIB browser test needs the
network or a Foundry credential, the stand-in has been bypassed and the test is wrong.

## Non-goals in this matrix

- No live Foundry scenario: that is `e02`'s live tier.
- No load or concurrency scenario beyond one double-submit: the guard is a cost control, not a
  rate limiter.
- No mobile-specific scenario beyond what the existing builder tests already cover.
