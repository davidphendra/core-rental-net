# 0008 — The run has no deadline, and the endpoint writes its own answer

**Status:** accepted
**Date:** 2026-09-25
**Deciders:** product owner, engineer

## Context

The run endpoint carried a `RunBudget` — a class that existed for one reason, stated in its own remark:

> **Two tokens, because one cannot say which ending happened.** A run can be cancelled because the customer
> stopped it … or because it ran out of time. … Both arrive as `OperationCanceledException`, and the only thing
> that tells them apart is which token was signalled.

`Customer` was the caller's token; `Run` was that token **linked to a deadline** built from
`AgentFoundry:TimeoutSeconds` (default 45, chosen to sit above the p95 target). `Run` was what the whole run
used, and `Customer` was only ever consulted to answer the one question above.

That shape reached four places, so removing it is not a local edit:

1. `ISuggestionAgent.StreamAsync(SuggestionRequest, RunBudget)` — the port took the budget, deliberately, so a
   deadline would start before the catalogue was searched rather than after.
2. `FoundrySuggestionAgent.Relay` — `RunStreamingAsync(…, cancellationToken: budget.Run)` and
   `.GetAsyncEnumerator(budget.Run)`, with `catch (OperationCanceledException) when
   (budget.Customer.IsCancellationRequested)` rethrowing a customer's stop as an ending and letting a spent
   budget fall through as a failure.
3. `SuggestionRun.RunAsync` — the same two-way catch, plus `FailBestEffortAsync(budget)`, which wrote the
   failure on the **customer's** token because the run's was cancelled by definition.
4. `BuilderController` — `RunBudget.Over(HttpContext.RequestAborted, settings.TimeoutSeconds)`, the only use the
   controller had for `AgentFoundrySettings` at all.

The product owner's ask: merge the private `WriteAsync` into the action, remove `RunBudget`, and let the agent
decide how long a run may take — "the host has nothing to do with it".

## Decision

**D1 — `RunBudget` is removed, and the port takes the customer's token.** `ISuggestionAgent.StreamAsync` is now
`(SuggestionRequest request, CancellationToken cancellationToken)`, and `FoundrySuggestionAgent`,
`NoAgentConfigured` and `SuggestionRun` all carry a plain token. Nothing outside this Host ever referenced
`RunBudget`, so no other project is touched.

**D2 — The application sets no deadline.** How long a run may take is the agent's to decide. `AgentFoundrySettings`
loses `TimeoutSeconds` and `DefaultTimeoutSeconds`, the configuration read goes with it, and
`AgentFoundry:TimeoutSeconds` is removed from `appsettings.json`. Accepted consequence: **a run that would never
end now ends when the customer's connection does**, and nothing else. There is no longer any bound the
application enforces on its own paid call, in the same spirit as [0007](0007-the-run-is-not-guarded-or-validated.md),
which removed the bound on how many of them a customer may have at once.

**D3 — The customer-stop rule survives, and becomes unambiguous.** A cancellation that carries the customer's
token is still the ordinary ending it always was: the record says `Stopped`, nothing is written to a page that
is no longer there, and the exception is rethrown to be swallowed by the action. What changes is that this is no
longer a *judgement between two possibilities* — with no deadline there is no second reason for the token to be
signalled — so the conclusion `RunBudget` was built to make is now made by the code simply not having another
case to consider.

**D4 — A cancellation that is not the customer's is still answered.** The two-way catch is kept, with its second
arm re-purposed: an `OperationCanceledException` on a token that was never signalled is the transport giving up,
which is a **failure** rather than an ending, so the record says `Unavailable` and `FailBestEffortAsync` writes
`failed: unavailable` — best-effort, because it is written after the run is over. Dropping this arm would let
such an exception escape the action after the response had started, where the framework can neither send a status
code nor a body, and the customer would simply see the stream stop with no reason.

**D5 — The private `WriteAsync` is merged into the action.** `Suggest` now opens the stream, names the customer,
runs, and treats a stop as an ordinary end — in one method. It returns no result and writes to the response
itself, which is what MVC supports for a held-open answer; the remarks that used to sit on the helper moved onto
the action, because the action is what owns the rule now.

**D6 — The port's token parameter carries `[EnumeratorCancellation]`, and the compiler is what said so.** Both
implementations of `StreamAsync` are async iterators, and a plain `CancellationToken` parameter on one is **not**
the token a consumer passes through `GetAsyncEnumerator`/`WithCancellation`: CS8425 is raised because such a
token would be silently unconsumed. Recorded because the failure mode is exactly the kind that hides — a
consumer's cancellation would be ignored with no error anywhere.

**D7 — `AgentFoundrySettings` no longer reaches the controller.** With the timeout gone it had no other use
there, so it was removed from the constructor rather than left as an unused dependency; the agent adapter still
takes it, and the registration is unchanged.

## Consequences

**Good**

- Two collaborators fewer on the run's path: the controller depends on the agent, the request builder and the
  logger, and `RunBudget` and its linked `CancellationTokenSource` no longer sit between the caller's token and
  the agent.
- One token instead of two, so the class of mistake `RunBudget` was written to prevent — passing the wrong token
  and reporting a stop as a failure — is no longer expressible.
- One method owns the endpoint's whole behaviour, and its remarks are on the thing a reader opens first.

**Bad / recorded**

- **Nothing bounds a run's duration.** A hung or very slow agent now holds the response and the connection until
  the customer's browser gives up. Previously the application cut the run at 45 seconds, recorded it as
  `Unavailable`, and told the customer to try again.
- **"Too slow" is no longer a recorded outcome.** `AiRunVerdict.Unavailable` and `SuggestionEventStream.Unavailable`
  both described it; their remarks were rewritten to drop it, and the evaluation tier can no longer separate a
  slow run from an unreachable one in the record.
- **The `Stopped` / `Unavailable` split now rests on a single token**, with no second signal to cross-check it.
- **`AgentFoundry:TimeoutSeconds` is removed from the shipped configuration.** A deployment that set it is now
  setting something nothing reads; `appsettings.Local.json` was left alone (gitignored, development-only).
- **Nothing here is verified at the browser tier**, which is red at HEAD for the reason
  [0006](0006-the-run-endpoint-is-a-controller.md) D6 gives — and the browser tier is where a stopped run and a
  hung run would look different to a customer.

## Alternatives rejected

| Alternative | Why rejected |
|---|---|
| Keep `RunBudget` but default the timeout to unlimited | Keeps two tokens and a linked source to express a case that no longer exists |
| Keep the deadline, moving it into the agent | The agent is a separate process the Host does not control; a deadline the Host does not enforce is not a bound |
| Pass the deadline as a `TimeSpan` instead of a class | Still a deadline the application enforces, which is what the ask removes |
| Drop the second catch with the budget, so a non-customer cancellation escapes | Escapes the action after the response has started: no status code, no body, and a stream that just stops |
| Keep `FailBestEffortAsync` on the run's token | There is no run token; the branch is reached exactly when the customer's token is *not* signalled, which is what makes the write land |
| Leave `AgentFoundrySettings` on the controller | An injected dependency nothing uses, which reads as a rule that is still being enforced |
| Move `WriteAsync`'s remarks onto `Refuse` or a new comment block instead of the action | The cancellation rule is the action's, and a rule stated away from the thing that obeys it is how the two drift |

## Baselines

Measured on 2026-09-25, in the same working tree as [0006](0006-the-run-endpoint-is-a-controller.md) and
[0007](0007-the-run-is-not-guarded-or-validated.md).

| Baseline | Before this change | After |
|---|---|---|
| `BUILD` | `dotnet build CoreRentalNet.sln` — **0 warnings, 0 errors** | **0 warnings, 0 errors** |
| `NONBROWSER` | **712 passed, 0 failed** | **712 passed, 0 failed** (no test added or removed) |
| `BROWSER` | **red at HEAD**: 118 failed, 1 passed, 1 skipped — `Not built: …CoreRentalNet.E2E.LocalAgent.dll` | **unchanged**, same cause |
| `AGENT` | not run; nothing under `agentfoundry/` is touched | **40 passed, 0 failed** |

## Notes

- **The deadline had no test either.** Like the guard and the validator removed in
  [0007](0007-the-run-is-not-guarded-or-validated.md), nothing in the suite exercised the timeout, the
  `Spent` property or the two-token distinction — so removing them removes no coverage that existed, and the
  behaviour they encoded is now simply gone rather than proven equivalent.
- **`[EnumeratorCancellation]` is the one thing here that a compiler caught rather than a test.** It is worth
  remembering for any future `IAsyncEnumerable` port in this application: without it, a consumer's cancellation
  is accepted and ignored.
- **The customer-stop path is still covered**, by
  `AiBuilderEndpointTests` at the browser tier (AIWB-27/28/49 by their own record) — which cannot run at HEAD.
  The in-process suite covers the route, the gate and the refusals, not a mid-run stop.
- **What a slow run now looks like to a customer** is worth deciding deliberately the next time this endpoint is
  touched: the page will wait indefinitely, and the only thing that ends it is the customer stopping it.
- **D4 was narrowed afterwards.** [0009](0009-the-run-is-the-endpoint.md) dissolved `SuggestionRun` into the
  endpoint and removed the best-effort write from the second catch, having established that the adapter funnels
  every non-customer failure into an `Unavailable` event and that reaching the stream from a `catch` would need a
  nullable field. The catch survives as a record-only guard; the customer is no longer told.
