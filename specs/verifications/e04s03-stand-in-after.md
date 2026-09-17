# e04s03 — the stand-in agent, and what a real client does with it

**Date:** 2026-09-16
**Branch:** `e03-catalogue-contract`

## What was run

| Check | Before | After |
|---|---|---|
| BUILD `CoreRentalNet.sln` | 0 warnings, 0 errors | **0 warnings, 0 errors** |
| NONBROWSER | 591 passed, 0 failed | **601 passed, 0 failed** |
| BROWSER | 104 passed, 1 skipped | **104 passed, 0 failed, 1 skipped** |
| `agentfoundry/AgentFoundry.sln` | 38 passed | **38 passed** |

## What the stand-in is

`tests/CoreRentalNet.E2E.LocalAgent/` — a process that serves `POST /responses` with the Responses
protocol's event stream. It is the other end of the wire, written from the specification: numbered
`response.output_text.delta` events carrying the agent's own contract one line at a time, then one
`response.completed`.

| Route | What it is for |
|---|---|
| `POST /responses` | the agent's answer, streamed |
| `POST /scenario` | what the tests drive: the scenario the next run should act |
| `GET /scenario`, `GET /health` | what is available, and ready to be asked |

Five scenarios: `essential` (four stages, three tiers of three lines), `exhausted`, `rejected`,
`unavailable` and `malformed`. The last carries a line that is not JSON **and** a stage outside the
closed vocabulary, because the adapter's promise — that a message it cannot read costs a line of
progress — would otherwise never meet a bad message on the wire.

Two ways to run it: **as a process**, which is how the browser suite starts it before both hosts and
points them at it with `Agent__Endpoint`, and **in-process** on a port the test chose, which is how the
client tests get it without a process to wait for and reap. Both are the same agent.

## The finding worth the trip

**A hand-written event stream is read correctly by the real client.** The `OpenAI` package's Responses
deserializers are `internal`, so the format cannot be read off the SDK — but a server written to the
*documented protocol* and a client built the way the application builds it agree, and the long result is
the case that proves it: three options of three lines each arrive whole across piecewise events.

That is the risk this story existed to retire. Seven tests now hold the application's own registration —
`AddFoundryAgent`, not a client built around it — against the stand-in:

| Assertion | Why it is the one that matters |
|---|---|
| four stages in order, then the result | the protocol's events map onto the contract's kinds |
| the whole result, across the events | a truncated read still yields *a* result, just a smaller one |
| `exhausted`, `rejected`, `unavailable` arrive as themselves | an outcome is not a failure to read one |
| the unparseable line is gone, the run still finishes | the adapter's promise, on the wire |
| no endpoint configured | the operation exists and reports itself unconfigured |

## What the build corrected

1. **Two `Program` classes, one assembly.** Top-level statements in the stand-in generate a `Program`
   in the global namespace, and this project is compiled into the same test assembly as the
   application's `Program` — so every reference to either became ambiguous. The stand-in now has a named
   entry point, and its four types are in four files.
2. **The adapter passes an unknown stage id through, by design.** I had written the test the other way
   round, expecting the transport to filter. It does not, and should not: the closed list belongs to the
   agent's schema, and the application's answer to a stage it has no words for is to **render nothing** —
   which is `SuggestionStageCopy`'s test, not the adapter's. The test now asserts what actually holds:
   the unreadable line is dropped, everything readable is passed through in order.
3. **`Agent:Name`, not `Agent:AgentName`.** A test's mistake, found by asking why `IsConfigured` was
   false while the stream still ran.

## The guard the fixture needs

`ScenarioLibrary` names SKUs, and a SKU the catalogue does not have would be dropped by the application
**in every scenario** — so every scenario would exercise the drop path, the happy path would be tested
nowhere, and nothing would say so. `LocalAgentFixtureTests` holds every SKU in every scenario against
`src/shared/data/products.json`, and would fail the day one is renamed.

## Not done, deliberately

- **The page that drives this** — e04s03's remaining half, and next.
- **No session, anywhere.** The stand-in answers one request at a time with no memory of the last.
