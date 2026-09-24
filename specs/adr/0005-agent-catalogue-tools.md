# 0005 — The catalogue is published as MCP tools, and the agent reads it through them

**Status:** accepted
**Date:** 2026-09-22
**Deciders:** product owner, engineer

## Context

[0004](0004-catalogue-similarity-search.md) gave the catalogue a **second** search — by meaning, behind its own
permission — and left it reachable only by a machine caller over REST. The workspace-suggestion agent
(`agentfoundry/`) was **tool-less**: it composed from a catalogue pushed into its request, and could not ask the
catalogue anything. The application-side AI builder and the Discovery shortlist that fed it were removed in the
same working tree, so nothing in the repository called the agent at all.

The ask: let the agent call the catalogue's **name search** and **similarity search**, with **RBAC on the
tools**, the tools **discoverable**, and the tool calls **authenticated with Auth0**.

Three facts shaped the decision:

1. The agent is a **code-based Foundry hosted agent**: our process runs in the Foundry sandbox, so a tool may be
   an in-process function, a client-side MCP tool, or a Foundry-hosted tool.
2. Microsoft's guidance for hosted agents is to reach external tools through a **Foundry Toolbox** and
   `AddFoundryToolboxes`. That path routes the tool call through a Foundry **project connection**, whose `oauth2`
   type has no way to send Auth0's required `audience` request parameter — and the application validates the
   audience (`RequireAudience = true`), so a token without it is refused. The product owner chose to treat the
   Toolbox path as **unavailable** rather than verify it.
3. The Host's authorization is **claim-type-agnostic**: `ClaimAuthorizationHandler` compares a configured
   `ClaimType`/`ClaimValue` against the authenticated principal. So the same permission can be enforced on a new
   surface by reusing the policy, not by restating the rule.

## Decision

**D1 — The Host publishes the catalogue as an MCP server; the agent consumes it with the MCP C# client.** The
Host maps `/mcp` through `ModelContextProtocol.AspNetCore`; the agent connects with `McpClient` and hands the
discovered tools to the suggestor. This is a deliberate **deviation** from the hosted-agent guidance, and its
reason is Auth0's audience (Context, point 2). The Host's surface is plain MCP, so a future Toolbox revisit needs
no change there.

**D2 — Per-tool RBAC is the existing policy pipeline, not a second rule.** Each tool method carries
`[Authorize(Policy = …)]` using the **same** policy its REST endpoint carries — `CatalogApiPolicy` for the name
search, `SimilaritySearchPolicy` for the meaning search. `AddAuthorizationFilters()` makes the MCP server honor it:
`tools/list` is **filtered to what the caller may use** and `tools/call` is **refused** for the rest, both through
the same `IAuthorizationService` and the same `ClaimAuthorizationHandler`. A caller therefore **discovers only the
tools its token entitles it to** — which is the "RBAC on the tools" the request asked for, and which an in-process
function tool cannot provide because it is fixed at agent construction.

**D3 — The endpoint requires an authenticated caller, by a named policy.** `McpPolicy` requires an authenticated
principal and names the bearer scheme only where one is registered. It is a **named** policy so the hermetic suite
can re-point it at its own scheme, exactly as the API's policies already are; a gate written inline on the endpoint
would be the one thing the suite could not exercise.

**D4 — Two tools, each one operation, each its own type.** `SearchCatalogueTool` (`search_catalogue`) and
`SearchSimilarityCatalogueTool` (`search_similarity_catalogue`). Different permission, different handler, different
failure mode — the repository's one-public-operation rule and its "what is gated and what is offered stay one
list" rule both point at two types rather than one with a mode.

**D5 — Tools take the catalogue's vocabulary as words, and refuse what is not in it.** The MCP path does not pass
through `[ApiController]` model-state validation, so `CatalogToolFilters` parses `category`/`subCategory` with
`CatalogApiEnumHelper` and refuses an unknown word with `CatalogApiParameterHelper.Refusal` — the same vocabulary
and the same refusal the REST endpoint uses. A blank or over-long term is refused too. A model's arguments are
untrusted input; a word that is not one of the catalogue's is **never defaulted**.

**D6 — A failure is answered as a failure, and the operator's reason never crosses.** `CatalogToolFailure` logs the
detail and throws a safe sentence instead. `ProductSimilarityUnavailableException` carries the vector file's path,
which the REST endpoint already keeps out of its body; a tool answer is spent in a model's context and a
transcript, so the same rule applies harder. **Measured:** the MCP server answers a thrown tool with a generic
line of its own rather than the exception's message, so the safe sentence is a second line of defence rather than
the model's wording. There is no fallback to the name search — the rule [0004](0004-catalogue-similarity-search.md)
D5 already set.

**D7 — The tools are additive; the request keeps its catalogue.** `SuggestionRequest.catalogue` is **unchanged**,
and the pushed catalogue **keeps `metadata`**. The consequence is recorded rather than hidden: the tools do not
reduce the payload, they add the ability to search by meaning and to refine. The pushed shape (with metadata) and
the tool shape (without) are now deliberately different, because the push is matched against by the model and the
tool result is ranked by the server.

**D8 — The compact projection changes: no `metadata`, currency on the row.** `CompactCatalogItem` becomes
`Sku, Category, Name, SubCategory, Description, PricePerMonth, Currency`, and `CompactCatalogCollection` **keeps**
its `currency` as well. This reverses two decisions the compact view carried: `metadata` no longer survives (the
meaning search matches against it server-side), and currency is no longer "once on the envelope and never on a
row". It is stated on both because they serve two readers: the envelope is the only currency an **empty** answer
has, and the row is what a **tool answer read on its own** must carry. The projection is shared by the REST compact
view and the tools, so the change reaches both.

**D9 — Agent authentication is an Auth0 M2M client, refreshed before expiry.** The agent is a confidential client
acting as itself: `client_credentials` with the catalogue's API identifier as the audience, cached with a
two-minute margin and refreshed **before** a 401 rather than after one. **Recorded risk:** the client secret is a
long-lived credential in the agent container. Mitigations: it is read from configuration, sent only to the token
endpoint, never logged or echoed, and never written into the repository; the M2M grant holds only the two
permissions it needs. A secretless alternative — adding an Entra scheme to the Host so the agent uses its Foundry
agent identity — is recorded as the production upgrade and **not implemented**.

**D10 — Absent configuration means no tools, not a broken agent.** All five `CatalogTools__*` values are required
together. An unconfigured deployment has an agent with no tools rather than half-open access; a **configured**
server that cannot be reached stops the host, because an agent with no tools would answer every run from memory.

## Consequences

**Good**

- The agent can search the catalogue by name and by meaning, and the two permissions are enforced where they are
  already enforced — the same policies, the same handler, one rule.
- Discovery is permission-filtered, so the model is never shown a tool its token cannot use.
- The catalogue's use cases have one implementation with three surfaces (pages, REST, MCP), and the tool answer is
  the compact envelope the REST API already publishes.
- The Host no longer references the Foundry or Azure SDKs at all — the adapter that needed them was removed — so
  the MCP package is the only thing added.

**Bad / recorded**

- **The Host now depends on an MCP server package**, and the agent on an MCP client. Both are pinned at **1.2.0**,
  the same major on both sides of one wire.
- **A second convention to learn**: `[McpServerTool]` beside `[Authorize]`, and the rule that a new tool must have
  one (an architecture test now enforces it).
- **The tools do not save payload.** D7 keeps the catalogue in the request, so a run still carries it. The tools'
  value is relevance and refinement, not token reduction — which was the original reason retrieval existed.
- **Nothing was verified against a model or a Foundry project.** No live project was available, so the tool
  schemas, the structured-output-plus-tools interaction and tool-call quality remain unproven until one is.

## Alternatives rejected

| Alternative | Why rejected |
|---|---|
| A Foundry Toolbox with `AddFoundryToolboxes` | The recommended path for hosted agents, but its `oauth2` connection has no way to send Auth0's `audience`, and the target API requires it (Context, point 2) |
| A generic Foundry `oauth2` connection carrying a fixed token | A static token expires; a refresh would have to live in the connection, which cannot express it |
| In-process function tools calling the REST endpoints | Cannot provide permission-**filtered discovery** — the tools are fixed at agent construction — and would put a second HTTP hop in front of every search |
| A tool per search mode on one type | Different permissions, handlers and failures; the one-operation-per-type rule puts them in two |
| Exposing the similarity score on the tool result | Not stable across models and not meaningful on its own, for the reason 0004 already gives |
| Falling back to the name search when the vectors are unusable | Turns a retrieval outage into a bad answer, silently — 0004's rule |
| Letting a tool answer carry the operator's reason | A tool answer is spent in a model's context and a transcript; the vector file's path is written for an operator |
| Removing the pushed catalogue (pure pull) | The product owner kept it (D7); the trade is recorded rather than assumed |
| Adding an Entra scheme and using the secretless agent identity | Correct for production, but needs a tenant and a Host change; recorded as the upgrade, not taken now |

## Baselines

Recorded at the end of this change:

| Baseline | Measured |
|---|---|
| `BUILD` | `dotnet build CoreRentalNet.sln` and `dotnet build agentfoundry/AgentFoundry.sln` — **0 warnings, 0 errors** each |
| `NONBROWSER` | **671 passed, 0 failed** across the nine offline projects (was 664 after the baseline repair; +6 for the MCP surface, +1 for the MCP gate architecture test) |
| `AGENT` | **73 passed, 0 failed** (was 63; +4 tool wiring, +3 token, +2 unconfigured source, and one prompt-version change) |
| `BROWSER` | not run; nothing user-visible changed |

The ten projects of [0004](0004-catalogue-similarity-search.md) are nine now: the Discovery unit-test project was
removed with the shortlist it tested, and the Host's builder tests with the builder.

## Notes

- **The MCP server answers a thrown tool with its own generic line.** Measured through the hermetic MCP tests: the
  tool result is `An error occurred invoking 'search_similarity_catalogue'.`, never the exception's message. That is
  why D6 calls its sentence a second line of defence.
- **`/mcp` is mounted outside `/api`** so the API's problem-details pipeline cannot reshape a JSON-RPC error.
- **`AIUse` is read again, and this note records it changing.** It was a leftover when the builder was removed;
  the builder has since been restored, and `AiPolicy` guards the section and the endpoint with the claim. What
  `specs/rename-remainder.md` recorded as dead is corrected there.
- **The E2E suite still starts the deleted stand-in agent.** It is untouched by this change and does not run in the
  agreed definition of done, but the BROWSER baseline is red until it is dealt with.
