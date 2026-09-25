# 0010 — The run is one method, and the framework refuses the body

**Status:** accepted
**Date:** 2026-09-25
**Deciders:** product owner, engineer

## Context

[0009](0009-the-run-is-the-endpoint.md) dissolved `SuggestionRun` into `BuilderController`, leaving the streaming
half as a private `WriteAsync`. The ask was to merge that in too, so the whole run is one method.

**The budgets make that a subtraction, not a move.** Measured: `Suggest` was **36 of 40 raw lines** and
`WriteAsync` was ~25, so absorbing it needed ~20 lines removed from a method that had 4 to spare. The method
budget counts raw lines — in-body comments included — so there was nothing to gain by tightening prose.

What the action could give up was its **body refusal**: a six-line check plus a nine-line `Refuse` helper. And
that is the pattern this codebase already uses in the endpoint beside it — `CatalogController`'s own remark says a
filter that is not one of the catalogue's words "is refused by `[ApiController]` with a problem-details `400`
before the action runs".

## Decision

**D1 — `WriteAsync` is merged into `Suggest`.** The action now opens the stream, writes the stages, builds the
request and consumes the agent's events, with the two endings and the record after them. It is **37 of 40 raw
lines**, and the endpoint is one method plus two private helpers — four since
[0011](0011-the-payload-is-built-by-the-endpoint.md) merged the request builder in.

**D2 — The body's requirement is declared on the wire type, and `[ApiController]` refuses it.** `Query` carries
`[Required]`, so a body that omits it, or sends a sentence of nothing but spaces, is refused with a `400` before
the action runs. The in-action check and the `Refuse` helper are gone. A JSON `null` body is refused by the same
machinery, because the parameter is non-nullable, so the action can never see a null `ask`.

**D3 — The attribute target is `param:`, and the compiler does not tell you.** A record exposes both the
constructor parameter and the property, and MVC **refuses to read validation metadata from the property**:

> `InvalidOperationException: Record type 'WorkspaceQueryRequest' has validation metadata defined on property
> 'Query' that will be ignored. 'Query' is a parameter in the record primary constructor and validation metadata
> must be associated with the constructor parameter.`

Written as `[property: Required]` it **compiles cleanly and returns 500 on every request that binds a body** —
including the happy path, because the throw happens inside model validation. It was found by capturing the
exception the exception-handler middleware logs, not by reading the code, and the record type now carries the
finding so the next person does not repeat it.

**D4 — The declared `400` becomes `ValidationProblemDetails`.** That is what the framework actually sends, and
`CatalogController` declares the same for its own `400`. **This is a contract change**: the refusal body keeps the
`code` member (`api.invalid_request`, set by `ApiProblemDetails`) and the `traceId`, and gains an `errors` member
naming the offending member.

**D5 — The two cancellation catches collapse into one.** [0009](0009-the-run-is-the-endpoint.md) removed the
best-effort write, so the only difference left between them was which verdict they set. One `catch`, one verdict
expression, decided by whose token was signalled — the same behaviour, stated once instead of twice.

**D6 — The customer hash moves into the `finally`.** It was computed before the `try` and passed along; it is now
computed where the record is written, which is the only thing that uses it. Same value, one fewer local.

**D7 — The run's record now has a test.** `AiRunLog` had no coverage at all
([0007](0007-the-run-is-not-guarded-or-validated.md) and [0009](0009-the-run-is-the-endpoint.md) both record
that). A run now writes exactly one line, the line carries the verdict, and the `customerId` on it is asserted to
be a 64-character SHA-256 digest rather than any string — precise, so the assertion cannot pass because the field
is empty.

## Consequences

**Good**

- The endpoint is one method: the route, the gate, the body, the stream, the endings and the record are all in
  the thing a reader opens first.
- `BuilderController` drops from 128 to **108 code lines**, and the file no longer carries a helper whose only
  job was to hold an if-statement.
- The body's requirement is declared on the type that carries it, which is where a caller reading the contract
  finds it, and the `400` is now the same shape as the catalogue endpoint's.
- The refusal is `[ApiController]`'s, so it happens before the action for the same reason every other refusal on
  this API does.

**Bad / recorded**

- **The `400` body changed** (D4). A caller that branches on `code` is unaffected; one that parses the body
  strictly sees a new member. Recorded rather than presented as internal.
- **The refusal now depends on getting an attribute target right, and a wrong one is a `500`** (D3). The
  endpoint's own check was unbreakable and visible; the framework's is one keyword away from failing every
  request, and nothing at compile time or in the unit suite would have said so before this change's own test run.
- **`Suggest` is 37 of 40 raw lines — 3 lines of headroom.** [0009](0009-the-run-is-the-endpoint.md)'s warning
  stands and is now tighter: the next thing added to this action has to be added somewhere else.
- **Nothing here is verified at the browser tier**, which is red at HEAD for the reason
  [0006](0006-the-run-endpoint-is-a-controller.md) D6 gives.

## Alternatives rejected

| Alternative | Why rejected |
|---|---|
| Merge `WriteAsync` and let `Suggest` exceed 40 raw lines | `CodingStandardTests` fails, and a green baseline is this repository's definition of done |
| Keep the in-action refusal and extract the preamble into a helper | Adds back the kind of helper these five changes have been removing, to preserve a check the framework already performs |
| Keep `[property: Required]` | Returns 500 on every request that binds a body (D3) |
| Declare the `400` as `ProblemDetails` while the framework sends `ValidationProblemDetails` | The document would describe a body the endpoint does not send — the thing `CatalogController`'s content-type declarations exist to prevent |
| Drop the body requirement entirely and let a blank sentence reach the agent | A run costs roughly 172k input tokens; the refusal is what stops an empty sentence being spent on |
| Keep two catches and delete their comments to save the lines | The distinction is the point of the two; stating it once in a verdict expression is better than stating it twice without explanation |

## Baselines

Measured on 2026-09-25, in the same working tree as
[0006](0006-the-run-endpoint-is-a-controller.md)–[0009](0009-the-run-is-the-endpoint.md).

| Baseline | Before this change | After |
|---|---|---|
| `BUILD` | `dotnet build CoreRentalNet.sln` — **0 warnings, 0 errors** | **0 warnings, 0 errors** |
| `NONBROWSER` | **712 passed, 0 failed** | **713 passed, 0 failed** (+1: the run's record) |
| `BROWSER` | **red at HEAD**: 118 failed, 1 passed, 1 skipped — `Not built: …CoreRentalNet.E2E.LocalAgent.dll` | **unchanged**, same cause |
| `AGENT` | not run; nothing under `agentfoundry/` is touched | **40 passed, 0 failed** |

## Notes

- **`CapturingLoggerProvider` now keeps the exception, and that is how D3 was found.** Its entries carried the
  formatted message and the structured properties but dropped the `Exception`, and the exception handler's
  message is only "An unhandled exception has occurred while executing the request." — so a `500` in this suite
  was diagnosable by status code and nothing else. The entry now carries it. That is a test-harness change worth
  keeping: it is the difference between "the request returned 500" and the actual reason.
- **The record line, in full, from the test that asserts it** — `latencyMilliseconds` is 0 because the run ends
  in under a millisecond with no agent configured:
  `Suggestion run <id>: {"runId":"<id>","query":"a desk and a chair","payloadHash":"","model":"","promptVersion":"","modelCalls":0,"inputTokens":0,"outputTokens":0,"latencyMilliseconds":0,"rawOutput":"","verdict":"unavailable","customerId":"<64 hex>"}`
- **What a caller now gets for a blank sentence** is a `400` whose `errors` member names `Query`, with `code`
  still `api.invalid_request`. Verified in `BuilderApiTests`, which asserts the code and that nothing was
  streamed.
