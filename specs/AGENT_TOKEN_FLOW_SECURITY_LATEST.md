# Agent ↔ catalogue access-token flow — security position

**Status:** the analysis behind [ADR-0012](adr/0012-the-caller-token-travels-on-the-invocation.md), updated to what was actually built. The decision and its rejected alternatives live in the ADR; this document is the threat model, the measurements, and the residual risk.
**Last revised:** 2026-09-30, after the carrier change landed.

---

## 0. The short version

**What changed:** the caller's token stopped being a field of anything the agent is prompted with, is now held once
in one scoped object per run, and is given up at a named point that a test asserts. The transport that presents it
to the catalogue stopped trusting the framework's defaults and now refuses to send it anywhere it was not meant
for.

**What did not change:** **the token keeps its application-wide audience.** A leaked token still opens everything
its holder may reach, not `read:catalog`. The narrowing — a catalogue API with its own audience and permission,
which this analysis recommended as the fix — was ruled out by the product owner on tenant cost. So the abuse case
this document was written about is **reduced in likelihood and unchanged in impact**.

**The honest score.** The metric matrix below puts the delivered design at **≈36/60**, up from **30**, and the
option this analysis recommended at **50**. The work that shipped is the *hygiene* half — where the credential
lives and how long we keep it — not the *blast-radius* half. It should not be read as the second.

---

## 1. The flow as built

```
 browser ──(1)──► web app ──(2)──► Foundry hosted agent ──(3)──► catalogue MCP server
   Auth0            Auth0 token        reads it once, at              [Authorize(Policy)]
   login            in the session     the input stage                read:catalog / SimilaritySearch
```

| Hop | Carrier | Where it is decided |
|---|---|---|
| **1** browser → app | the sign-in cookie; the app reads the user's access token from the session | `BuilderController` → `SessionTokenExtensions.GetAccessTokenAsync()` |
| **2** app → agent | **`x-client-caller-access-token`**, a header the hosting layer forwards (its request context exposes *"the client headers, those prefixed with `x-client-`"*) | `CallerAccessTokenInvocationPolicy`, installed once on the pipeline both clients share; read by `CallerAccessTokenHeaderReader` at the input stage |
| **3** agent → MCP | `Authorization: Bearer`, attached per request | `CallerCatalogueAccessTokenAttachmentHandler`, gated by `CatalogueServerRequestPolicy` |

The application no longer puts the token in the payload, the agent's wire record no longer declares one, and the
schema no longer publishes one. There is **nothing to lift back out of a message**, so the splitter, its
interface, the marker interface and the decorator's generic parameter are gone — and the guardrail remains as a
tripwire against a future change that reintroduces the carrier.

---

## 2. What was bought, mapped to the goal

| Property | How it is met | Proven by |
|---|---|---|
| **P1** the token is not a field of anything the agent is prompted with | the header carrier; the three deleted types; the member removed from both records and from `suggestion.request.schema.json` | the `x-client-` prefix pinned on each side, and a workflow test whose request carries **no** token and whose run still succeeds |
| **P2** held once, in one place, for the smallest window the design allows | one scoped `IMcpAccessTokenService` per call; no cache, no session, no second copy | four tests on the holder, including the nested-scope restore |
| **P3** the moment it stops being used is explicit | `Release()` called by all three endings **before** the ending is announced, logged, with `Dispose()` as the backstop | a theory across `success`, `rejected` and `unavailable` |

Plus what the transport work fixed on its own account: no redirect can carry the bearer elsewhere, no cookie jar is
shared between calls, revocation is actually checked, a request outside the configured origin is **refused** rather
than sent unauthenticated, an unreachable catalogue endpoint fails at startup rather than inside a paid-for run,
and a connection-pool leak that abandoned a handler on every call was closed.

---

## 3. What was not bought — the residual risk

| Risk | Detail | Status |
|---|---|---|
| **Audience is application-wide** | The credential is audienced for this whole application. RFC 9700 §2.3: *"access tokens SHOULD be audience-restricted to a specific resource server … the resource server MUST refuse to serve the respective request."* §4.10.2: *"audience restriction limits the impact of token leakage."* The MCP authorization specification: *"MCP servers MUST validate that tokens presented to them were specifically issued for their use"*, and *"token passthrough is explicitly forbidden."* Our MCP server validates the same authority and audience the rest of the application does, and re-checks the caller's permission per tool — so the token is *valid* there. It is simply **wider than the resource** | **unchanged** — narrowing declined |
| **No sender constraint** | Bearer only: no DPoP (RFC 9449), no mTLS binding (RFC 8705). A leaked token is replayable for its whole lifetime. RFC 9700 §4.10.1 prefers sender-constrained tokens for exactly this | **unchanged** |
| **Nobody in the agent can renew it** | With no exchange, its lifetime is Auth0's and no code here can refresh it. A run that outlives its token fails at the catalogue and ends `unavailable` | **by design** — and the reason a run deadline and TTL arithmetic were removed rather than added: those would be *us* deciding validity |
| **At-rest persistence** | The invocation is recorded by the hosting layer, and client headers are *"persisted verbatim"* in its recovery payload. So the token is written down by the platform even though it is no longer a message | **not measured** — see §5 |

---

## 4. Abuse cases, updated

| # | Abuse case | Before | Now |
|---|---|---|---|
| A1 | Someone with log/trace access replays the token | possible — in the request body | **narrowed**: not in a message, but headers are persisted by the platform, so this rests on the platform's own controls |
| A2 | Someone with access to the durable store replays it | unverified | **still unverified** (the deployed store was never inspected) |
| A3 | A model echoes the token into the customer's answer | mitigated by a splitter **and** a tripwire | **by construction**: nothing puts it in a message. The guardrail is kept as a tripwire against regression |
| A4 | A leak at the tool opens the rest of the application API | possible | **unchanged** — the audience was not narrowed |
| A5 | The MCP endpoint redirects and the bearer follows | not prevented (framework defaults) | **prevented**: no redirects, origin-pinned, refused rather than sent |
| A6 | An intermediary replays the token after the run | possible | **unchanged** — bearer, no sender constraint |
| A7 | The agent's own credentials exceed the caller's rights | not applicable (it has none) | still not applicable |
| A8 | A caller reaches a tool they are not entitled to | prevented — per-tool `[Authorize]` | **unchanged** — still the strongest property, and the reason the agent-identity designs (which would give it up) were not taken |

---

## 5. Measurements never taken

Three were planned and all three were dropped when the product owner removed deployments from the plan. They are
recorded as debt, not as done:

- **Does the Foundry proxy forward an `x-client-` header?** One deployed probe. **P1 rests on the documented rule
  and on nothing measured.** If a deployment shows the header is dropped, the symptom is a retriever offered no
  tools and a run ending `unavailable` — the reader logs `absent` on the invocation, which is the first line to
  read. The fix would be a carrier change, not a rewrite: the token is already a parameter through the controller,
  the run service and the adapter, and only the last hop states it.
- **What does the deployed host persist for a run?** Rides on the same deployment. Without it, A1/A2 stay
  unverified.
- **Does the toolbox proxy present the caller's authority or its own?** The one design that removes the credential
  from our process entirely. Still available; never probed.

Two numbers that *were* measured, because they decided a design:

- The **name-field weight sweep** (2/5/10) found the weight is not the lever — 5 and 10 answer identically and 2
  merely trades one product's rank for another's — so the pool's bound of fifteen is a property of the terms and
  the indexed fields, not of a tuning number. `NameFieldScoreBoostSweepTests` holds it, and it pins the shipping
  constant to the measured value.
- The **release**, across all three endings, is asserted rather than described.

---

## 6. The option matrix, with what was built

Scores 0–5, higher better. *As built* is the delivered design; the other columns are the analysis that informed it.

| Metric | **O0 as it was** | **As built** | O1 sample pattern | O3 narrowing |
|---|---|---|---|---|
| M1 secret-carrying hops | 1 | 1 | 1 | 3 |
| M2 at-rest exposure | 1 | 2 | 3 | 3 |
| M3 model-context exposure | 2 | **5** | 5 | 4 |
| M4 audience & scope narrowing | 1 | 1 | 1 | 5 |
| M5 blast radius if leaked | 1 | 1 | 1 | 5 |
| M6 replay window & revocation | 2 | 2 | 2 | 4 |
| M7 confused-deputy resistance | 3 | 3 | 3 | 4 |
| M8 per-caller attribution | 5 | 5 | 5 | 5 |
| M9 spec compliance | 2 | 2 | 2 | 5 |
| M10 implementation risk (higher = safer) | 2 | **4** | 3 | 3 |
| M11 added attack surface | 5 | 5 | 5 | 4 |
| M12 operability | 5 | 5 | 5 | 4 |
| **Total /60** | **30** | **36** | **36** | **50** |

**Read the M4/M5/M9 row as the whole argument of this document.** The rows that moved are M3 (the token is no
longer a message) and M10 (nothing left to forget to strip). The rows that decide what a leak *costs* are
untouched. A reader who wants the blast radius reduced should start from the narrowing, not from this work.

---

## 7. Where each control lives, for whoever audits this next

| Control | Type |
|---|---|
| `CatalogueServerRequestPolicy` | the origin/scheme rule; the loopback exception is decided from configuration, not the environment |
| `CallerCatalogueAccessTokenAttachmentHandler` | refuses an unapproved request; attaches the bearer |
| `McpAuthenticationHelper` | `AllowAutoRedirect = false`, `UseCookies = false`, CRL on the TLS options, bounded pooled lifetime, the client disposed by the transport |
| `CallerAuthorisedMcpSettings.FromConfiguration` | fails at startup on an endpoint an MCP client cannot reach |
| `IMcpAccessTokenService` / `McpAccessTokenService` | the single holder; `Release()` at every ending, `Dispose()` as the backstop |
| `CallerAccessTokenHeaderReader` | the only place the token enters a run; presence logged, never the value |
| `NoWireContractCarriesASecretTests` | no published contract may declare a member that is a credential — the exception list is now empty |
| The log-hygiene test | no log line a run writes contains the token |
| `ModelOutputGuardrailChatClient` | the tripwire: fails a run whose answer repeats the token |

---

## 8. Sources

- MCP authorization specification (2025-06-18) — <https://modelcontextprotocol.io/specification/2025-06-18/basic/authorization>
- RFC 9700, *OAuth 2.0 Security Best Current Practice* — §2.3, §4.10.1, §4.10.2
- MAF samples: `Agent_MCP_PerRun_AuthHeaders` (the transport checklist), `Agent_MCP_Server_Auth`,
  `FoundryAgent_Hosted_MCP`, `ResponseAgent_Hosted_MCP` (the toolbox and the platform-owned MCP call)
- `Microsoft.Agents.AI.Foundry.Hosting`: `HostedCallContext`, `HostedSessionContext`, `FoundryToolboxService`,
  `FoundryJsonCheckpointStore` — the platform behaviour these conclusions rest on
- `Azure.AI.AgentServer.Responses`: `ResponseContext.ClientHeaders` (the `x-client-` rule) and
  `ResponseRecoveryPayload.ClientHeaders` (*"persisted verbatim"*)
- RFC 8693 *Token Exchange*, RFC 8707 *Resource Indicators*, RFC 9449 *DPoP*, RFC 8705 *mTLS-bound tokens* —
  the narrowing routes that were declined
