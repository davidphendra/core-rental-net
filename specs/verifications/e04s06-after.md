# e04s06 — one run at a time

**Date:** 2026-09-16
**Branch:** `e03-catalogue-contract`

## What was run

| Check | Before | After |
|---|---|---|
| BUILD `CoreRentalNet.sln` | 0 warnings, 0 errors | **0 warnings, 0 errors** |
| NONBROWSER | 633 passed, 0 failed | **643 passed, 0 failed** |
| BROWSER | 116 passed, 1 skipped | **118 passed, 0 failed, 1 skipped** |
| `agentfoundry/AgentFoundry.sln` | 38 passed | **38 passed** |

## What the guard is

One run in flight per **principal**, plus a cooldown, both configured rather than hard-coded. It is a
cost control on the section and not a rate limiter on the application: it limits how often a run
*starts*, never how long one runs — and one submitted request is already bounded at three attempts by the
agent. The realistic way ten model calls become twenty is a double click, a slow page and an impatient
second Enter, which is what this absorbs.

**Process-local, and correct here rather than a shortcut.** The README states the application must never
scale out, so there is no second process for this state to disagree with. A distributed guard would buy
nothing and cost a network round trip on the path it exists to make cheaper.

The slot is released on **every** terminal path — completion, failure, cancellation and the circuit going
away. A guard that outlived its run would punish a customer for using the cancel control this epic added
for them.

## Two things it corrected

1. **A bad configuration value used to stop the application.** `Configuration.GetValue<double>` *throws*
   on a value it cannot convert, so a typo in the cooldown would have prevented startup — over a cooldown,
   which is neither the failure anybody wants nor one they would connect to the setting that caused it.
   The value is read as text and parsed, falling back to the default.
2. **The cooldown leaked between tests.** `A_double_submit_is_refused_by_the_page` passed alone and
   failed in the suite, because the cooldown is measured from when a run *starts* and every AI test signs
   in as the same account — so a previous test's run refused the next one. The suite now runs with a zero
   cooldown, which is the honest configuration for it: the browser holds the **one-in-flight** rule, and
   the cooldown has unit tests with values it chooses. The `Task.Delay(4s)` that was papering over this
   is gone.

## The tests

| Test | What it holds |
|---|---|
| AIB-22 | a second submit while a run is in flight is refused — driven twice, for the double click and the second Enter |
| `One_customers_run_does_not_block_anothers` | keyed on the principal, so one account's run never refuses another's |
| `The_cooldown_outlives_the_run` | releasing is measured from when the run started, or a customer could restart the instant one ended |
| `The_cooldown_comes_from_configuration` | configured, and nonsense values fall back rather than stop the application |
| AIB-23 | a **cancelled** run releases the slot: the next submit is accepted immediately, with no wait |
| `A_double_submit_is_refused_by_the_page` | the refusal is the application's own words, and nothing reaches the agent for it |

## Acceptance criteria

- AIB-22 and AIB-23 pass. **Yes.**
- A cancelled run releases the guard. **Yes**, asserted with no wait.
- The guard is per principal and configured, not hard-coded. **Yes.**
- The three baselines are green. **Yes.**
