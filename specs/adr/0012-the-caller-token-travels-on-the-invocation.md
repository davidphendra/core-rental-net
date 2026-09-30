# 0012 — The caller's catalogue token travels on the invocation, is held once, and its release is a step

**Status:** accepted
**Date:** 2026-09-30
**Deciders:** product owner, engineer

## Context

The caller's access token used to reach the agent **inside the request payload** — a member named
`mcpAccessToken` on the record the application sent — and the agent took it back out before any model saw it:

- `WorkspaceSuggestionRequestPayload.McpAccessToken` (application) → serialized into the run's message;
- `SuggestionRequest.McpAccessToken : ICallerTokenCarryingRequest<SuggestionRequest>` (agent), with
  `WithoutAccessToken()` existing solely so the token could be removed;
- `CallerTokenRequestSplitter<TRequest>` rewriting the message on **every** model call, and
  `CallerAuthorisedMcpChatClient<TRequest>` lifting the token into a call-scoped `IMcpAccessTokenService`;
- `ModelOutputGuardrailChatClient`, which fails a run whose answer repeats the token — a tripwire whose existence
  was the honest measure of the risk, because the design's failure mode was *the token in the model's context*.

The product owner's objection was direct: *"I don't like the way we pass through token."* The question asked was
narrowed to three properties, and everything else was explicitly out of scope:

- **P1** the token is not a field of anything the agent is prompted with;
- **P2** it is held once, in one place, for the smallest window the design allows;
- **P3** the moment it stops being used is explicit.

Two constraints were set by the product owner and shaped everything below: **no Auth0 configuration change** and
**no narrowing of the token's audience** — so no catalogue API, no token exchange, no second agent client.

## Decision

**D1 — The token travels on the invocation, on a header the platform forwards.** The application states it as
`x-client-caller-access-token`; the agent reads it once, at the input stage — the only stage still inside the HTTP
request, because the workflow may hand a later stage to another thread. The `x-client-` prefix is the hosting
layer's own forwarding rule: its request context exposes *"the client headers (those prefixed with `x-client-`)"*,
so a name outside that prefix is dropped, and a dropped token is indistinguishable from a catalogue refusal. A
test pins the prefix on **both** sides, because the two are separate deployables with no shared assembly and the
name is stated twice.

The application states it through a **pipeline policy installed once** on the base options both clients share,
reading the run's own scope as each request is built. The client is built once and shared; the run is not, so the
token cannot be a property of the client.

**D2 — Nothing is lifted out of a message, so nothing has to be.** `CallerTokenRequestSplitter`,
`ICallerTokenRequestSplitter` and `ICallerTokenCarryingRequest` are deleted, `WithoutAccessToken()` with them, and
`CallerAuthorisedMcpChatClient` **loses its generic parameter** — the type parameter's only purpose was to name
the request whose token the splitter had to recognise. The token leaves the application's payload record, the
agent's wire record, and `suggestion.request.schema.json`; the payload hash loses its
`with { McpAccessToken = null }` special case, because the bytes that are hashed are now the bytes that are sent.

`NoWireContractCarriesASecretTests` enforces D2 rather than describing it: no published contract may declare a
member whose last name-word is a credential's. The one exception it carried was emptied with this change, and the
test would have failed had the exception outlived the member.

**D3 — The token is held once, in one place, and given up at a named point.** `IMcpAccessTokenService` holds it
for the call and gained `Release()` and `IDisposable`, which mean different things:

- `Release()` — the run saying it has finished with the credential. All three endings (`success`, `rejection`,
  `unavailable`) call it **before** announcing the ending, so nothing after an ending can reach for it. It logs
  *"the caller's catalogue token was released at the end of the run"* and a second release is deliberately not an
  event.
- `Dispose()` — the call saying it is over. Silent, and the backstop: the holder is registered scoped, so a path
  that never reaches an ending still cannot keep a token.

A theory across all three endings asserts the holder is empty and the release line was written; four further tests
pin the holder's own behaviour.

**D4 — The transport stops trusting the framework's defaults.** A bearer token is attached to every request the
MCP client makes, so the defaults that would undermine it are turned off: `AllowAutoRedirect = false` (a redirect
would carry the token to an origin the policy never approved), `UseCookies = false` (a jar is state shared between
calls on a transport that exists for one call), revocation checking (`SocketsHttpHandler` carries it on its TLS
options, where the default is to check nothing), and a bounded pooled lifetime. A `CatalogueServerRequestPolicy`
decides where the token may go at all — the configured origin, over a transport that protects it, with plain text
permitted only for a loopback endpoint — and
`CallerCatalogueAccessTokenAttachmentHandler` **refuses** a request the policy did not approve rather than sending
it unauthenticated.

Two defects were found while doing this: the transport was told `ownsHttpClient: false` and nothing ever disposed
the `HttpClient`, so **every call leaked a handler and its connection pool**; and the application's catalogue
endpoint was validated only when it was first used, inside a paid-for run.

## Consequences

**What this buys.** The token is no longer in a message — not in a payload a model may be shown, not in the bytes
the host records with the request, not in the payload hash. It is held in one scoped object per call, read from
one place, and given up at a point a reader can find and a test can assert. The transport refuses to send it
anywhere it was not meant for. `ModelOutputGuardrailChatClient` remains as a tripwire, now guarding against a
future change that reintroduces the message carrier rather than against the current design.

**What this does not buy, and it should not be read as bought.** The token keeps its **application-wide
audience**. A leaked token still opens everything its holder may reach, not `read:catalog`, so RFC 9700 §2.3 and
the MCP specification's audience requirement remain unmet; the narrowing was offered and declined on cost. Nobody
in the agent process can renew the token either — with no exchange, its lifetime is Auth0's — so a run that
outlives its token fails at the catalogue and ends `unavailable`, which is where it already was.

**The one property accepted unverified.** Whether the Foundry proxy forwards an `x-client-` header **was not
measured**; no deployment was available and none is required by the product owner's decision. P1 therefore rests
on the documented forwarding rule. If a deployment shows the header is dropped, the symptom is a retriever offered
no catalogue tools and a run ending `unavailable`, diagnosable from the reader's `absent` line on the invocation —
and the fix would be a carrier change rather than a rewrite, because the token is already a parameter through the
controller, the run service and the adapter, and only the last hop states it.

**Not committed.** This change is entirely in the working tree, and the deletions D2 makes are not recoverable
from git.

## Alternatives rejected

| Alternative | Why it was rejected |
|---|---|
| **Narrowing the token** — a catalogue API with its own audience and `read:catalog`, accepted by `/mcp` only | Needs Auth0 tenant work (an API, a permission, a user grant, an agent client) which the product owner ruled out. The design does not lose the option: it is configuration and three values, not a redesign |
| **An exchange (OBO) in the agent**, so the tool receives a catalogue-audienced token and validity is the provider's | Needs the same tenant work plus a confidential client in the agent. This is where `TokenCredential` and MSAL were planned, and both were dropped by decision — the product owner's constraint removed the reason for either |
| **Treating "short-lived" as a shorter token lifetime** — a run deadline plus a freshness check, with the TTL chosen against a measured worst case | That is *us* deciding validity, which the product owner rejected: *"I don't want our code to do the validation, but rather OAuth provider to do it."* The deadline, the margin arithmetic and the acquisition-time check were all removed. `ADR-0008` (the run has no deadline) therefore still stands |
| **The session-state carrier** — an `AIContextProvider` reading the token from `AgentSession.StateBag`, which the MAF README offers as an alternative | `AgentSessionStateBag.Serialize()` is public and the hosting layer persists sessions and checkpoints workflows, so a raw secret there is a token at rest — the thing this ADR exists to avoid. It is also not a channel the application can write: the hosted session is created and keyed server-side. The *provider* remains the right construct; what it should carry is `HostedSessionContext.UserId`, which is an identity and not a credential |
| **The Foundry toolbox** — the platform owns the MCP call and a project connection holds the credential, so nothing of ours holds a token at all | The only design that removes the credential entirely, and it stays available. It needed a deployed probe to answer whether the proxy resolves the caller or presents its own identity, and the product owner removed deployments from the plan |
| **Correcting two remarks that described the exchange** (`CallerAuthorisedMcpSettings`, `IMcpAccessTokenService`) | They were true before the exchange was dropped and are still true after: the process holds no identity of its own, and the holder holds the caller's token |

## Baselines

Recorded at the end of this change:

| Baseline | Measured |
|---|---|
| `BUILD` | `dotnet build CoreRentalNet.sln` — **0 warnings, 0 errors**; `AgentFoundry.sln` likewise |
| `NONBROWSER` | **776 passed, 0 failed** across the offline projects (Integration **136**, Host **227**, Architecture **61**, plus BuildingBlocks 32, Rentals 94, Catalog 70, Workspace 80, BehaviourLock 37, CatalogIngestion 39) |
| `AGENT` | **118 passed, 0 failed** |
| `BROWSER` | not run; unchanged by this work |

`ARCHITECTURE` is among the ten and passes, including the rules this change made load-bearing: one type per file,
the 40-line method budget (which caught the refusal written into the controller), and
`NoWireContractCarriesASecretTests`, whose exception is now empty.

## Notes

**The three properties, and where each is proven.** P1 by the deletions and the two prefix tests plus a workflow
test whose request carries **no** token at all and whose run still succeeds — so the token that reached the
catalogue can only have come off the invocation. P2 by the holder's scope and the four tests on it. P3 by the
theory across all three endings.

**What the release made unobservable.** Because the ending releases the token, no test can assert what the holder
*contained* after a run — which is the point, and is why the proof of P1 is a run succeeding without a token in
its message rather than a post-run inspection.

**Two tests caught mistakes of mine, and one caught a bug that would have shipped silently.** `Problem(...)` in an
action that returns no result sets nothing — a result that is never executed is never applied — so the refusal
would have reached the customer as a successful, empty stream; the test asserting a 403 got a 200. The
architecture suite's 40-line budget caught the controller method. And a test double built with a null logger made
an assertion measure nothing until the double was fixed to log through the caller's factory.

**One log line to look for.** `Caller catalogue token present|absent on the invocation` — the first thing to read
if a run ends `unavailable` where the catalogue was expected, because it says whether the carrier arrived.
