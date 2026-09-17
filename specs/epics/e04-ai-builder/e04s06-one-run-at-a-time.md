# e04s06 — A double submit cannot spend a second run

**type:** feat
**risk:** P1
**context:** host
**bcps:** 2
**status:** failing

## Context

One submitted request costs **up to ten model calls and an external round trip**. The realistic way
that multiplies is not malice — it is a double click, a slow page, an impatient second Enter. Two
accidental submits is twenty calls.

The guard is **process-local**, which is correct here precisely because the README states the
application **must never scale out** — so in-memory state cannot diverge across instances.

## Requirements

#### ADDED: one in-flight run per principal, and a short cooldown

The Host allows **one run in flight per authenticated principal**, plus a short cooldown between runs,
both enforced server-side where the identity is read. A refused second submit is reported in the
application's own words. The guard is **released when a run ends, including when it is cancelled** —
otherwise cancelling would punish the customer.

## Zoom-Out

- **Module purpose:** a cost control on the section, not a rate limiter on the application.
- **Callers:** the submit path of the AI section.
- **Contracts to preserve:** nothing outside this story; the port's behaviour is unchanged, and the
  guard never alters an answer.

## Steps

1. Add the in-flight guard keyed on the principal, with its cooldown as configuration. → verify: `dotnet build CoreRentalNet.sln -v q --nologo`
2. Release the guard on completion and on cancellation. → verify: `dotnet test tests/CoreRentalNet.Host.Tests --nologo --filter "FullyQualifiedName~RunGuardTests"`
3. Report a refused submit with app-owned copy. → verify: `dotnet build CoreRentalNet.sln -v q --nologo`
4. Unit test AIB-22 and browser test AIB-23. → verify: `dotnet test tests/CoreRentalNet.E2E --nologo --filter "FullyQualifiedName~AiBuilderGuardTests"`
5. Re-run the three baselines. → verify: `dotnet test CoreRentalNet.sln --filter "FullyQualifiedName!~CoreRentalNet.E2E" --nologo`

## Verification Script (Step-by-Step)

1. Submit a request and submit again immediately → the second is refused with a message, and only one
   run reaches the agent.
2. Cancel a run, then submit again immediately → the second is accepted.
3. Wait out the cooldown and submit → accepted.
4. Confirm the guard is per principal: a second account is unaffected.

## Test matrix

| ID | Scenario | Level |
|----|----------|-------|
| AIB-22 | A second submit while a run is in flight is refused | unit |
| AIB-23 | The guard is released when the run ends, including on cancel | browser |

## Out of scope

- A per-day quota, which would need persistence and a schema change.
- Rate limiting on the catalogue API, which is unchanged by this epic.
- Concurrency across accounts: the guard is per principal by design.

## Risks

- **A guard that outlives its run.** A crash mid-run would hold it; the cooldown bounds the damage, and
  the guard is released on every terminal path including cancellation.
- **State shared across principals.** Keyed on the principal, asserted by AIB-22's second-account case.
- **A guard mistaken for a rate limiter.** It limits how often a run starts, not how long one runs —
  and a run is already bounded at three attempts by the agent.

## Acceptance criteria

- AIB-22 and AIB-23 pass.
- A cancelled run releases the guard.
- The guard is per principal and configured, not hard-coded.
- The three baselines are green.
