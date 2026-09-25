# 0006 — The suggestion run is a controller, and the controller is internal

**Status:** accepted
**Date:** 2026-09-25
**Deciders:** product owner, engineer

## Context

The run endpoint (`POST /api/builder/suggest`) was the application's **one** minimal-API endpoint, and it was
deliberate: its own remark said so. "A minimal-API endpoint rather than a controller. The answer is held open
and written to for as long as the run takes, which is not the shape an MVC action is written for, even though
the catalogue's endpoint beside it is a controller." `ApplicationPipelineExtentions` repeated the argument as a
comment and mapped it with `app.MapSuggestionEndpoint()`.

The ask: rewrite it as a controller deriving from `ControllerBase`, in `Controllers/`, **self-contained** — no
port, no separate run class.

**Three facts about MVC govern the answer**, and none of them was visible from the existing code:

1. `ControllerFeatureProvider.IsController` requires `typeInfo.IsPublic`. **An `internal` controller is never
   discovered.**
2. A public member may not name a parameter type less accessible than itself (`CS0051`) — verified against the
   compiler both ways: a public method taking an internal type fails, a public method on an internal type does
   not.
3. `AssemblyPart.Types` is `Assembly.DefinedTypes`, so **internal types are visible to a feature provider**, and
   `ActivatorUtilities` selects from `type.GetConstructors()` (public constructors) **without requiring the type
   to be public** — so an internal controller with a public constructor over internal dependencies is both
   discoverable and constructible.

Fact 2 is what decides the shape. The run needs `ISuggestionAgent`, `AgentFoundrySettings` and
`SuggestionRequestBuilder`, all internal on purpose. (It also needed `RunGuard`, `SuggestionValidator` and
`RunBudget` when this was written; [0007](0007-the-run-is-not-guarded-or-validated.md) removes the first two and
[0008](0008-the-run-has-no-deadline.md) the third.) A public controller cannot name them, and widening them does
not stop at three: `ISuggestionAgent.StreamAsync` takes `SuggestionRequest` and returns
`IAsyncEnumerable<AgentSuggestionEvent>`, so a public `ISuggestionAgent` pulls in the whole `Agents/` and
`AiBuilder/` graph — 20+ types.

Replacing the constructor with a service locator was rejected: property injection is not an option either
(`DefaultControllerPropertyActivator` activates only `[ActionContext]`/`[ControllerContext]` properties), so it
would have meant `HttpContext.RequestServices` in the action, which hides the dependencies the composition root
is supposed to own.

## Decision

**D1 — The endpoint is `BuilderController`, and the streaming is unchanged.** An action that writes to the
response and returns no result is how MVC expresses a held-open answer, so nothing about the stream changed: the
framing, the headers, the cancellation-as-an-ordinary-end rule and the four SSE events are the same code. The
gain is that the operation is declared where every other one is — a route attribute, a bound body, a gate and
its answers on the action — and `ApplicationPipelineExtentions` no longer maps it, because `app.MapControllers()`
already discovers it.

**D2 — The controller is `internal`, and the framework's discovery rule is widened by exactly that much.**
`InternalControllerFeatureProvider` admits an internal type that carries `[Controller]` **on itself**, and is
added beside the framework's own provider rather than replacing it, so the rule for public controllers is
untouched. `[ApiController]` derives from `[Controller]`, so the marker is already there on `BuilderController`
and no extra attribute is needed.

**D3 — Nothing was widened at all.** `WorkspaceQueryRequest` stays `internal` and the whole AI feature stays
internal. An earlier cut of this change kept the controller public and put the run behind a public
`ISuggestionRun` with an internal `SuggestionRunner`; that was withdrawn, because it widened the request record
and added two types to satisfy a rule that one deliberate declaration satisfies instead.

**D4 — The action declares its answers.** `[Produces("text/event-stream")]` and `[ProducesResponseType]` for
200, 400, 401, 403, 409 and 500, in the shape `CatalogController` already uses. The `409 builder.run_in_flight`
had no declaration anywhere before this. (Two of those have moved since: the `409` went with
[0007](0007-the-run-is-not-guarded-or-validated.md), and the `400` is a `ValidationProblemDetails` because the
framework sends it — [0010](0010-the-run-is-one-method.md).)

**D5 — The document's security gap is recorded, not closed.** `Auth0SecuritySchemeTransformer` declares the
required scope on the two catalogue paths only, so the builder operation is documented as ungated while the gate
is real. That gap is older than this change and closing it means teaching the transformer a third permission — a
separate decision, recorded rather than taken.

**D6 — The browser tier is red at HEAD, before this change and after it, for a reason of its own.**
`a6e127e feat(mcp)!` deleted `tests/CoreRentalNet.E2E.LocalAgent` (9 files, 408 lines) while
`HostFixture.StartLocalAgent` and `TestPaths.LocalAgentAssembly` still start it, so the whole E2E fixture fails
to initialize: **118 failed, 1 passed, 1 skipped**. [0005](0005-agent-catalogue-tools.md) already recorded it.
Restoring that project is separate work and was **not** taken here; the endpoint's own coverage moved to the
in-process tier instead.

## Consequences

**Good**

- The route is declared like the rest of the API, and a reader finds the gate and the answers on the operation.
- No `app.MapSuggestionEndpoint()` call, and no "this one route is different" comment in the pipeline.
- **The AI feature is now entirely `internal`** — better than the state this change started from, where the
  request record had to be public for the controller to bind it.
- The refusals have executable coverage they never had: 403 without the permission, 400 for a blank sentence,
  400 for a body that binds to nothing, and 409 while a lease is held — deterministically, in process. The
  browser tier cannot reach the 409 reliably and, at HEAD, cannot run at all.

**Bad / recorded**

- **Controller discovery is now an application-wide rule.** Any `internal` type carrying `[Controller]` or
  `[ApiController]` is routable. It is opt-in through that attribute and not "any internal `*Controller`", and
  `InternalControllerFeatureProviderTests` pins both directions — but it is a rule a reader has to know.
- **An `internal` controller is unusual**, and someone will eventually try to make it public. That is now a
  compile error rather than a tidy-up, and the type's own remark says why.
- **Two HTTP behaviours change at the boundary**, both from `[FromBody]` and both stated rather than hidden: a
  request whose `Content-Type` is missing or unsupported is answered **415** where the minimal endpoint read the
  JSON regardless, and a JSON `null` body is refused by the run rather than risking a 500. Both in-app callers
  (`ai-run.js` and `AiBuilderEndpointTests`) send `application/json`, so neither path is exercised by the
  application's own clients.
- **The browser tier proves nothing about this change right now** (D6).
- **The document describes a gated operation as ungated** (D5).

## Alternatives rejected

| Alternative | Why rejected |
|---|---|
| A public controller with the dependency graph widened to match | `ISuggestionAgent` alone pulls in the entire `Agents/`+`AiBuilder/` graph through its members: 20+ files, and the feature loses the boundary it was written with |
| Keeping the run behind a public port (`ISuggestionRun` + internal `SuggestionRunner`) | Tried first and withdrawn: it widened the request record and added two types to satisfy a framework rule, when one declaration on the controller satisfies it instead |
| A service locator inside the action (`HttpContext.RequestServices`) | Compiles and needs no framework change, but it hides the dependencies and contradicts *callers depend on the interface, never the class* |
| Property injection | Not supported: `DefaultControllerPropertyActivator` activates only `[ActionContext]`/`[ControllerContext]` properties |
| `AddControllersAsServices()` with an internal constructor and an explicitly ordered registration | Depends on registration order to beat `TryAddTransient`, and changes how **every** controller is activated, to serve one endpoint |
| A discovery provider that admits any internal `*Controller` | The widening would be a blanket one; requiring the attribute on the type itself is what makes it a declaration |
| Leave it as a minimal-API endpoint | It is what the product owner asked to change, and the stated reason for it — "not the shape an MVC action is written for" — turned out to be wrong: an action that returns no result is exactly that shape |
| Close the OpenAPI security gap here | A third permission in the document transformer is a contract change of its own; recorded as D5 instead |

## Baselines

Measured on 2026-09-25, in this working tree (which also carries unrelated in-flight work in `agentfoundry/` and
`Agents/`; the pre-change figures were taken on the same tree so the delta is attributable).

| Baseline | Before this change | After |
|---|---|---|
| `BUILD` | `dotnet build CoreRentalNet.sln` — **0 warnings, 0 errors** | **0 warnings, 0 errors** |
| `NONBROWSER` | **704 passed, 0 failed** across the nine offline projects | **713 passed, 0 failed** (+5: the run endpoint's boundary tests; +4: the discovery rule and its limit) |
| `BROWSER` | **red at HEAD**: 118 failed, 1 passed, 1 skipped — `Not built: …CoreRentalNet.E2E.LocalAgent.dll` (D6) | **unchanged**: 118 failed, 1 passed, 1 skipped, same cause |
| `AGENT` | not run; this work touches nothing under `agentfoundry/` | **40 passed, 0 failed** |

## Notes

- **The provider's rule had a bug, and the negative test caught it.** The first version looked for `[Controller]`
  with `inherit: true` — and `ControllerBase` itself carries `[Controller]`, so **every** subclass was admitted,
  including an unmarked internal probe type. Measured, not reasoned: the probe failed with
  `…UnmarkedProbeController` in the admitted set. The rule now looks for the attribute **on the type itself**,
  which is what makes the widening opt-in rather than a blanket one. Any future relaxation of that search has to
  keep the unmarked case failing.
- **What the refactor did not change.** The event frames, their order and their names; the headers that stop
  buffering; and the rule that a customer's cancellation is an ordinary end rather than a failure. All of it
  moved, none of it changed. (This list also carried the run's deadline and the per-customer lease; later work
  removed both — [0007](0007-the-run-is-not-guarded-or-validated.md) and
  [0008](0008-the-run-has-no-deadline.md).)
- **`[Produces("text/event-stream")]` is metadata only.** `ProducesAttribute.OnResultExecuting` returns early
  unless the result is an `ObjectResult`, and this action returns none, so the media type is still set by
  `SuggestionEventStream.BeginAsync` and the document still learns the truth.
- **The new tests are in `CoreRentalNet.Host.Tests`, in process.** The boundary tests need a scheme that can
  produce a principal carrying the permission's claim and an account the run's record can be built from. That
  handler was written for this suite because the `RunGuard` keyed a customer by `ClaimTypes.NameIdentifier` and
  refused a principal it could not name as unkeyable; with the guard gone
  ([0007](0007-the-run-is-not-guarded-or-validated.md)) the suite reuses the catalogue's `CatalogApiTestHandler`.
