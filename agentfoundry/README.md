# CoreRentalNet.Agents

**The hosted-agent deployable for CoreRentalNet: it serves Foundry agents over the Responses protocol, and turns one customer sentence into workspace setups drawn from the catalogue.**

> 🔗 For the product application and the whole-system view, see the **[root README](../README.md)**.

## Contents

| Part | Sections |
|---|---|
| **A — Orientation** | [1. Status](#1-title--status) · [2. What it is](#2-what-it-is) · [3. System at a glance](#3-system-at-a-glance) · [4. Repository map](#4-repository-map) · [5. New here? Start with this](#5-new-here-start-with-this) · [6. Requirements](#6-requirements) · [7. Quick start](#7-quick-start) |
| **B — Architecture** | [8. The host](#8-architecture--the-host) · [9. The agents](#9-architecture--the-agents) · [10. How it connects to the catalogue](#10-how-it-connects-to-the-catalogue) · [11. Domain glossary](#11-domain-glossary) |
| **C — Reference** | [12. Configuration](#12-configuration) · [13. Testing & quality gates](#13-testing--quality-gates) · [14. Build, versioning & CI](#14-build-versioning--ci) · [15. Deployment & operations](#15-deployment--operations) |
| **D — Help & process** | [16. Troubleshooting / FAQ](#16-troubleshooting--faq) · [17. Process & conventions](#17-process--conventions) · [18. Credits](#18-credits) |

---

## 1. Title & status

### 1.1 Name and one-line description

`CoreRentalNet.Agents` is the deployable that serves one or more **Foundry hosted agents**. A feature — its instructions, its stages and its graph — lives in one folder a newcomer can find without reading the whole project.

Its suggestion quality rests on the catalogue's **generated metadata**; see the [root README §3.6](../README.md#36-how-the-generated-metadata-aligns-a-query).

### 1.2 Status

The agent suite is measured by running it. No badge is used, because the run state is a file- and code-derived fact, not an external service.

| Signal | Where | State |
|---|---|---|
| 🧪 Agent tests | `dotnet test AgentFoundry.sln` | ✅ **199 passed / 0 failed** (2026-10-12) |
| 📦 Solution | `AgentFoundry.sln` | `src/CoreRentalNet.Agents` + `tests/CoreRentalNet.Agents.Tests` |
| 🤖 Agents served | `azure.yaml` | Two: `core-rental-workspace-suggestion-agent`, `echo-reverse-agent` |
| 🚀 CI / deploy | `azure-pipelines.yml` (agent tree) | Separate ADO pipeline; `azd provision` then `azd deploy` |

### 1.3 Back to the application

This deployable **consumes** the application: the catalogue lives in the CoreRentalNet application and is reached over MCP. For the product itself, start at the [root README](../README.md).

---

## 2. What it is

### 2.1 The deployable in one paragraph

The container binds `PORT` (default `8088`), serves readiness and the Responses protocol, and holds no catalogue credential of its own. Every run searches the catalogue **as the caller**, using a token carried on the invocation.

### 2.2 The two agents it serves

| Feature | Agent | What it does | Cost |
|---|---|---|---|
| `Features/WorkspaceSuggestion` | `core-rental-workspace-suggestion-agent` | Turns one customer sentence into workspace setups drawn from the catalogue | A model call per stage, several per run |
| `Features/EchoReverse` | `echo-reverse-agent` | **Reverses** the message it was sent (`ReverseNodeExecutor`) | Nothing — no model, no catalogue |

### 2.3 What it is not

- 🚫 It holds **no** catalogue credential — the caller's token is presented instead.
- 🚫 **No model decides the lifecycle** — whether another attempt runs is deterministic code.
- 🚫 A run with no token does **not** answer from memory; it reports `unavailable`.

### 2.4 Audiences

- 🤖 **Agent team** — `Program.cs`, `Hosting/`, `Shared/`, `Features/`.
- 🧱 **Application team** — the caller of the workspace endpoint; owns the catalogue and `/mcp`.

---

## 3. System at a glance

### 3.1 Relationship to the application

The application hosts the catalogue MCP server at `/mcp` and the `/suggestions` run endpoint. This deployable is the agent side of that boundary: it is configured with the MCP endpoint and searches through it.

The catalogue behind `/mcp` is itself generated — from the Hugging Face Amazon Reviews dataset by `CatalogPopulate`, then embedded by `CatalogIngestion`. See the [root README §8.6](../README.md#86-the-catalogue-provenance-generation-and-ingestion).

The catalogue answers through **two** searches, and the agent may use either: a **keyword search ranked by Okapi BM25**, and a **semantic search over vectors**.

### 3.2 One diagram

```
        👤 caller (browser or console)
             │  request carries  x-client-mcp-catalog-access-token
             ▼
 ┌─────────────────────────────────────────────────────┐
 │ 🤖 CoreRentalNet.Agents (this deployable)            │
 │   Foundry hosted agents · Responses protocol         │
 │    • core-rental-workspace-suggestion-agent          │
 │    • echo-reverse-agent                              │
 │   WorkspaceSuggestion workflow                       │
 │    6 model stages + deterministic policies           │
 └───────────────────────┬─────────────────────────────┘
                         │ MCP over HTTP — caller's token stamped per call
                         │
                         │ 🔧 search_catalogue            (read:catalog)
                         │      └─ keyword search · Okapi BM25 (Lifti)
                         │         over product name + description
                         │
                         │ 🔧 search_similarity_catalogue (SimilaritySearch)
                         │      └─ semantic search · vectors (sqlite-vec, cosine)
                         │         over product_embedding.db
                         ▼
 ┌─────────────────────────────────────────────────────┐
 │ 🏢 CoreRentalNet application (/mcp)                  │
 │   Catalog (products.json) · Discovery (BM25 + vectors)│
 └─────────────────────────────────────────────────────┘
```

### 3.3 The handshake in one paragraph

- 🔑 `AccessTokenHeaderReader` reads the caller's token off the invocation header; `IMcpAccessTokenService` holds it for the run; `McpAccessTokenHandler` stamps it on each MCP request.
- 🚫 The process holds **no** catalogue identity to configure — the endpoint is the whole of the MCP configuration.
- 🛑 No token ⇒ `CatalogueUnavailableException` ⇒ a run that ends `unavailable`.

### 3.4 What runs where

| Component | 💻 Local dev | ☁️ Hosted container | Who supplies it |
|---|---|---|---|
| Foundry project endpoint | `.env` → `FOUNDRY_PROJECT_ENDPOINT` | injected by the platform | Platform |
| Agent name | `FOUNDRY_AGENT_NAME` local default | injected by the platform | Platform |
| Model deployment | `AZURE_AI_MODEL_DEPLOYMENT_NAME` | mapped in `azure.yaml` | azd |
| Catalogue MCP endpoint | `CATALOGTOOLS__MCPENDPOINT` | mapped in `azure.yaml` | azd |
| Caller token | request header | request header | 👤 caller — never stored |

---

## 4. Repository map

### 4.1 Top level

| Path | What it holds |
|---|---|
| `AgentFoundry.sln` | The agent solution |
| `src/CoreRentalNet.Agents/` | The deployable's source |
| `tests/CoreRentalNet.Agents.Tests/` | The agent test suite |
| `shared/contracts/*.json` | JSON Schema for every stage contract |
| `azure.yaml` | The two `azure.ai.agent` services |
| `azure-pipelines.yml` | The agent tree's Azure DevOps pipeline |

### 4.2 Source tree

```
src/CoreRentalNet.Agents/
├── Program.cs                     the composition root: host steps and features
├── appsettings.json
├── Hosting/                       bootstrapping: settings · telemetry · model client ·
│                                   default agent · startup report · local .env
├── Shared/                        reused by every feature
│   ├── Agents/          AgentProfile · AgentFactory
│   ├── ChatClients/     the model call's decorators
│   ├── Configuration/   HostedAgentName
│   ├── Diagnostics/     AgentBuildIdentity · AgentVersion
│   ├── Guardrails/      Abstractions · Arguments · Pipeline · Results · Tools
│   ├── Mcp/             caller-authorised MCP: header reader · token holder · connection ·
│   │                    request policy · tool-answer ledger
│   ├── Model/           model client registration · transport options · run usage
│   ├── Prompts/         embedded instruction source
│   ├── Scoping/         IRunScope · RunScope
│   ├── Serialization/   ContractJson
│   ├── Telemetry/       build-identity processor · token-leak redaction · workspace telemetry
│   └── Workflows/       ChatEntryStageExecutor · StreamingStageExecutor · WorkflowOutputPublisher
└── Features/
    ├── WorkspaceSuggestion/       prompts · roster · domain · stages · policies · graph
    └── EchoReverse/               prompt-free · roster-free · one stage · graph
```

### 4.3 Test tree

`tests/CoreRentalNet.Agents.Tests/` — one project, 56 files, 199 tests. It covers the graph, the stage contracts, the guardrails, the MCP token handling, the host settings and the build identity.

### 4.4 Find it fast

| Want to find… | Look in |
|---|---|
| A stage's prompt | `Features/WorkspaceSuggestion/Prompts/<name>.vN.md` |
| A stage's identity & contract | `Features/WorkspaceSuggestion/Agents/WorkspaceSuggestionAgentRoster.cs` |
| A stage's code | `Features/WorkspaceSuggestion/Stages/Executors/` |
| A deterministic policy | `Features/WorkspaceSuggestion/Stages/DeterministicPolicies/` |
| A stage contract shape | `shared/contracts/*.schema.json` |
| The catalog tool guard order | `Features/WorkspaceSuggestion/WorkspaceSuggestionRegistration.cs` |

---

## 5. New here? Start with this

### 5.1 Reading path

1. **This file, §1–§3** — the shape of the deployable (10 min).
2. **`Program.cs`** — the composition root, one line per concern (5 min).
3. **`Features/WorkspaceSuggestion/WorkspaceSuggestionWorkflow.cs`** and `…AgentRoster.cs` — the graph and its stages (10 min).
4. **`Features/WorkspaceSuggestion/WorkspaceSuggestionRegistration.cs`** — what a feature registers (10 min).
5. **`Shared/ChatClients/`** and **`Shared/Guardrails/`** — the cross-cutting behaviour.

### 5.2 Documentation map

| Question | File |
|---|---|
| What is the whole system? | [root `README.md`](../README.md) |
| What are the mandatory coding/test rules? | [`../CONVENTIONS.md`](../CONVENTIONS.md) |
| What is the authority chain? | [`../CLAUDE.md`](../CLAUDE.md) |
| How was this deployable built and deployed? | this file, §14–§15 |

### 5.3 The authority chain

The repository's authority chain is stated in [`../CLAUDE.md`](../CLAUDE.md); it is not repeated here.

### 5.4 Where decisions live

There is no `specs/adr/` directory in this repository. The decisions that exist are recorded in `CONVENTIONS.md`, in `CLAUDE.md`, and in the code comments that explain *why*.

---

## 6. Requirements

### 6.1 Toolchain

- **.NET SDK `10.0.400`**, pinned in `../global.json`; the deployable targets `net10.0`.
- The repository's `Directory.Build.props` and `global.json` apply to this tree as well.

### 6.2 azd and the extension

- **`azd` ≥ `1.27.1`** and the **`azure.ai.agents` extension ≥ `1.0.0-beta.9`** (declared in `azure.yaml`; the pipeline pins azd `1.35.1`).

### 6.3 Foundry project and model

- A **Foundry project** endpoint (`FOUNDRY_PROJECT_ENDPOINT`) — required; injected in a container.
- A **model deployment** (`AZURE_AI_MODEL_DEPLOYMENT_NAME`, default `gpt-4.1-mini`).

### 6.4 Catalogue endpoint

- `CATALOGTOOLS__MCPENDPOINT` pointing at the application's `/mcp`. Absent ⇒ the agent cannot search rather than answering from memory.

---

## 7. Quick start

### 7.1 Build and test

```bash
cd agentfoundry
dotnet test AgentFoundry.sln
```

### 7.2 Run it locally

```bash
cd agentfoundry/src/CoreRentalNet.Agents
cp .env.example .env          # then fill FOUNDRY_PROJECT_ENDPOINT and AZURE_AI_MODEL_DEPLOYMENT_NAME
PORT=8088 dotnet run
```

Or through azd, **naming the service you intend to invoke**:

```bash
cd agentfoundry
azd ai agent run core-rental-workspace-suggestion-agent
```

The host binds `PORT` (default `8088`) and **ignores** `ASPNETCORE_URLS`.

### 7.3 Check it is ready

```bash
curl -s -o /dev/null -w '%{http_code}\n' http://127.0.0.1:8088/readiness      # 200
```

Startup prints, before anything else can fail, what a deployment must supply — each required setting as `set` or `MISSING` — the agent names it serves, and the build identity.

### 7.4 Smoke-test the echo agent

The echo agent **reverses** the message it is sent. It makes no model call and needs no catalogue.

```bash
azd ai agent run echo-reverse-agent                              # terminal 1
azd ai agent invoke echo-reverse-agent "hello there" --local    # terminal 2
```

Expected: the reversed text, in a fraction of a second.

### 7.5 Invoke the workspace agent with a caller token

A console sends a sentence, not the application's envelope, and **cannot search the catalogue** (it carries no caller token). Use the explicit request to exercise the pipeline:

```bash
curl -sS -N -H 'Content-Type: application/json' -H 'Accept: text/event-stream' \
  -H 'x-client-mcp-catalog-access-token: <the caller's token>' \
  -d '{"model":"core-rental-workspace-suggestion-agent","input":"{\"runId\":\"r1\",\"query\":\"a desk and a chair under 500000\",\"currency\":\"IDR\",\"ceilingMonthly\":500000,\"slots\":[{\"slot\":\"Desk\",\"capacity\":1},{\"slot\":\"Chair\",\"capacity\":1}]}","stream":true}' \
  http://127.0.0.1:8088/responses
```

A run streams one typed JSON event per line — see [9.6](#96-the-typed-event-stream).

### 7.6 End-to-end against the real catalogue

1. Start the application host (it publishes `/mcp`).
2. Mint a caller token that carries `read:catalog`.
3. Point the agent at it: `CATALOGTOOLS__MCPENDPOINT=<host>/mcp`.
4. Invoke as in [7.5](#75-invoke-the-workspace-agent-with-a-caller-token) and confirm real SKUs.

---

## 8. Architecture — the host

### 8.1 The hosting model

`Program.cs` is the composition root: a list of host steps and features, and no logic of its own. `AgentHost.CreateBuilder` binds the port and wires telemetry; `AddFoundryResponses` registers the Responses server; `RegisterProtocol` maps it. The **served agent is always the feature's workflow agent itself, unwrapped** — hosting can only redirect a hosted workflow's checkpoints when the agent it resolves is that workflow agent.

### 8.2 The folder law

**`Hosting/` bootstraps the host; `Shared/` is anything a second feature would use unchanged; everything else belongs to a feature.**

### 8.3 The composition root

The order in `Program.cs` is deliberate: settings are resolved once (`AgentHostSettings.FromConfiguration`), telemetry is added before anything emits it, the shared model client is built once, then the MCP token plumbing and run scope, then each feature registers itself, then the default agent.

### 8.4 `Hosting/` bootstrapping

| Concern | Where |
|---|---|
| Configuration, resolved once | `AgentHostSettings` |
| Telemetry providers and resources | `HostTelemetry` |
| Shared model client | `SharedModelClient` |
| The default agent fallback | `DefaultAgent` |
| The startup report | `StartupReport` |
| Local `.env` fill | `LocalEnvironmentDefaults` |

---

## 9. Architecture — the agents

### 9.1 Anatomy of an agent

Every agent is the same three artifacts, so adding one is a folder and a registration, not a new shape:

1. **A prompt** — `Features/<Feature>/Prompts/<name>.v<N>.md`, embedded into the assembly. The version lives in the file name, so a run can name the instructions it used. Only the active version is kept; superseding a prompt removes the previous file, and git history keeps its text.
2. **A profile** — an `AgentProfile` in the feature's roster: stage name, prompt file, description, output format, and whether it may search (`UsesCatalogueTools`).
3. **A stage** — an executor that calls its stage agent and publishes what came back.

`AgentFactory` builds the `AIAgent` from a profile and an `IChatClient`, which is what lets the whole tree run offline against a fake.

### 9.2 The workspace-suggestion graph

A run is a bounded workflow, not a straight line:

```
read request
     │
     ▼
verify workspace request ──not about a workspace──► complete: rejected
     │ about a workspace
     ▼
rephrase & expand requirement ◄──────────────────────────────────┐
     ▼                                                           │
retrieve catalogue products                                      │
     ▼                                                           │
build candidate product pool      (deterministic)                │
     ▼                                                           │
rerank per component              (best first, with a reason)    │
     ▼                                                           │
bound retrieved products          (deterministic: top-N, dedupe) │
     ▼                                                           │
compose workspace setups                                         │
     ▼                                                           │
validate setup structure          (deterministic: provenance)    │
     │ structurally invalid ─────────────────────────────────────┤
     │ structurally valid                                        │
     ▼                                                           │
review workspace setups                                          │
     ├── acceptable ──► complete: success (setups streamed first) │
     └── rejected ────► decide retry ── attempts left ─────────────┘
                              └── attempts exhausted ──► complete: unavailable
```

Three rules the graph enforces: the **verifier runs once**; retry returns to **rephrasing, not composition**; and **no model decides the lifecycle**.

### 9.3 Model stages

Six stage agents, each one prompt and one profile (`WorkspaceSuggestionAgentRoster`):

| Stage | Agent name | Prompt | Decides | Tools |
|---|---|---|---|---|
| Verifier | `workspace-request-verifier` | `workspace-request-verifier.v2.md` | Is this a workspace request at all? | none |
| Rephraser | `workspace-requirement-rephraser` | `workspace-requirement-rephraser.v2.md` | What does the customer want, and what words will find it? | none |
| Retriever | `catalogue-product-retriever` | `catalogue-product-retriever.v2.md` | What does the catalogue offer? | the catalogue's MCP tools — **BM25** keyword and **semantic** vector search, as the caller's token allows |
| Reranker | `catalogue-candidate-reranker` | `catalogue-candidate-reranker.v2.md` | Which of what it offered answers each component? | none |
| Composer | `workspace-setup-composer` | `workspace-setup-composer.v2.md` | Which products form a setup? | none |
| Reviewer | `workspace-setup-reviewer` | `workspace-setup-reviewer.v2.md` | Does the setup set satisfy the request? | none |

### 9.4 Deterministic policies

Code, not prompts — the decisions a model must not make:

- `WorkspaceComponentProductPoolPolicy` — one product per SKU, at most fifteen per component.
- `WorkspaceComponentProductSelectionPolicy` — the reranker's order, a SKU it was never given dropped.
- `WorkspaceComponentSearchVocabularyLimitPolicy` — bounds the words before the retriever sees them.
- `WorkspaceSetupCandidateStructureValidator` — every SKU came from the reranked set, quantities within capacity, components actually requested.
- `WorkspaceSetupRetryDecisionPolicy` — which of the three endings, and whether an attempt is left.

### 9.5 The echo agent

`echo-reverse-agent` **reverses** the caller's message (`ReverseNodeExecutor`) and does nothing else. It exists so hosting can be exercised from a console — routing by name, session handling, streaming — with no model call, no catalogue and no cost. It is a **workflow with one stage** on purpose: hosting treats an agent that runs a workflow differently from one that does not. It is enabled by default and switched off with `ECHOAGENT__ISENABLED=false`.

### 9.6 The typed event stream

No model text ever reaches a caller. A caller receives typed events, one JSON object each, discriminated by `type`:

| `type` | Carries | When |
|---|---|---|
| `stageStarted` | `processingStage` | a stage begins |
| `stageCompleted` | `processingStage` | a stage finishes |
| `retry` | `nextAttemptNumber`, `maximumAttemptCount` | a rejected attempt gives way to another |
| `candidate` | `approvedWorkspaceSetup` | an approved setup — **after** review, never before |
| `completed` | `runStatus`, `outcomeReason`, `completedAttemptCount`, `runUsage` | the run ends |

`runStatus` is one of `success`, `rejected`, `unavailable`, and is deliberately **separate** from the candidates: a setup's arrival must never imply a successful run. The `type` discriminator comes from `StreamingStageExecutor<TState, TEvent>`, so a stage cannot publish through a concrete type and silently drop it.

### 9.7 The chat-client decorator stack

Each model call is wrapped in an ordered stack — **telemetry → guardrail → MCP tools** — over a no-op leaf. `TelemetryChatClient` is the innermost, wrapping the guardrail: it owns the run's span, records one line and the token counts per call, names the stage from the call's options, and reports the run's total on the `completed` event. `GuardrailChatClient` fails the run loudly if an answer ever repeats the caller's token. `AuthorisedMcpChatClient` offers the catalogue tools only to a stage that declares `UsesCatalogueTools`.

### 9.8 Guardrails

The guard **wraps the tool itself, not the function loop** (`GuardedToolFunction`, a `DelegatingAIFunction`): the catalogue's tools reach the model's options from inside the chat-client chain, downstream of where MAF function middleware would transform tools, so a tool guarded there would be a tool guarded nowhere. The order is stated once, in `ToolGuardPipeline` and the composition root, not in MAF's middleware composition:

| Phase | Guards, in order |
|---|---|
| Arguments | `ToolAllowListGuard` → `ToolAuthorizationGuard` → `ArgumentCeilingGuard` → `ToolArgumentGuard` (the catalogue's category/subCategory vocabulary) |
| Results | `ToolResultSizeGuard` → `ToolResultSchemaGuard` → `UntrustedToolDataGuard` → `ToolResultRecordingGuard` (the ledger) |

The allow-list is also what decides which tools the model is offered. A denied argument guard stops the chain before the tool runs and the reason is **returned to the model**, not thrown, so it may correct the call.

---

## 10. How it connects to the catalogue

### 10.1 The catalogue as an MCP server

The application exposes **two searches** under `/mcp`, each its own tool and its own permission:

- `search_catalogue` — a **keyword search ranked by Okapi BM25** (Lifti's default scorer, `k1 = 1.2`, `b = 0.75`). It indexes the product **name** and its **description**, and answers a whole-word match before a word-start match. Gated by `read:catalog`.
- `search_similarity_catalogue` — a **semantic search over vectors**: the nearest passages by cosine distance in `product_embedding.db` (a `sqlite-vec` `vec0` table read with a `MATCH` query), reduced to each product's nearest passage. Gated by the `SimilaritySearch` claim (closed by default).

The two differ in what they can find: BM25 matches the **words** a product carries — the description is indexed so an expanded term can reach it — while the semantic search matches a sentence's **meaning**. Both narrow through the same catalogue operation, so a product above the caller's ceiling is ineligible to either.

The retriever is offered whichever tools the caller's token entitles, so the same run can discover one tool or two.

### 10.2 Server-authorised vs caller-authorised

The single decision that explains the cross-cutting code: the agent does **not** hold a catalogue credential. It presents the **caller's** token, so the catalogue enforces the caller's own entitlements.

### 10.3 The caller-token lifecycle

`AccessTokenHeaderReader` reads the token (`x-client-mcp-catalog-access-token`) off the invocation; `IMcpAccessTokenService` holds it for the run; `McpAccessTokenHandler` stamps it on each MCP request. It is **never logged** — a guardrail fails the run if a model answer repeats it, and `TokenLeakRedactionProcessor` scrubs it before export.

### 10.4 Permissions and gates

| Gate | Owner | Default |
|---|---|---|
| `CatalogRead` (`permissions` = `read:catalog`) | the application | enabled |
| `SimilaritySearch` | the application | **closed** |
| `WorkspaceSuggestion` | the application | **closed** |

An empty claim type/value means closed: the capability is refused, not open.

### 10.5 The failure mode by design

A run whose request carries no token reports `unavailable` (`CatalogueUnavailableException`) rather than composing from memory. That is the trust model, not a defect.

---

## 11. Domain glossary

### 11.1 Feature terms

| Term | Meaning |
|---|---|
| **Run** | One bounded workflow execution |
| **Stage** | One model or deterministic step in the graph |
| **Requirement expansion** | The rephraser's output: per-component query, terms, synonyms, semantic concepts |
| **Candidate** | An approved workspace setup, published only after review |

### 11.2 Stage terms

| Term | Meaning |
|---|---|
| **Verifier** | Decides whether the sentence is a workspace request |
| **Rephraser** | Produces the requirement expansion and the search vocabulary |
| **Retriever** | Searches the catalogue (the only stage with tools) |
| **Reranker** | Orders each component's products against its need |
| **Composer** | Builds candidate setups |
| **Reviewer** | Decides whether the setup set satisfies the request |

### 11.3 Event and status terms

| Term | Meaning |
|---|---|
| `stageStarted` / `stageCompleted` | A stage's boundaries |
| `retry` | A rejected attempt gives way |
| `candidate` | An approved setup |
| `completed` | The run ends, carrying `runStatus` |
| `success` / `rejected` / `unavailable` | The three run outcomes |

### 11.4 Term → code

Domain types live in `Features/WorkspaceSuggestion/Domain/` (events under `Domain/Events`, outputs under `Domain/Outputs`, tools under `Domain/Tools`). The stage identities live in `Agents/WorkspaceSuggestionAgentRoster.cs`.

---

## 12. Configuration

### 12.1 Agent names

| Setting | Default | Meaning |
|---|---|---|
| `FOUNDRY_AGENT_NAME` | `core-rental-workspace-suggestion-agent` | **Reserved prefix.** The platform injects the deployed name; the value here is the local default. Must equal the `azure.yaml` service `name`. |
| `EchoAgent:AgentName` | `echo-reverse-agent` | The name a console addresses the echo agent by |
| `EchoAgent:IsEnabled` | `true` | Set `false` to stop serving the echo agent |
| `AgentHost:DefaultAgentName` | the workspace agent's name | Which agent a request that names none gets. **Set per service in `azure.yaml`.** |

### 12.2 Model and transport

| Setting | Default | Meaning |
|---|---|---|
| `FOUNDRY_PROJECT_ENDPOINT` | *(empty)* | The Foundry project. Required; injected in a container |
| `AZURE_AI_MODEL_DEPLOYMENT_NAME` | `gpt-4.1-mini` | The deployment every stage runs on |
| `ModelTransport:NetworkTimeout` | `00:10:00` | How long one model call may take |
| `ModelTransport:MaximumRetryAttempts` | `2` | How many times one failed call is retried — **not** a run's attempt count |

### 12.3 Workspace workflow bounds

| Setting | Default | Meaning |
|---|---|---|
| `WorkspaceSuggestionWorkflow:MaximumAttemptCount` | `3` | How many times a run may rephrase and try again |
| `WorkspaceSuggestionWorkflow:MaximumRetrievedProductsPerComponentForReranking` | `15` | How many products each component offers the reranker |
| `WorkspaceSuggestionWorkflow:MaximumSelectedProductsPerComponent` | `3` | How many products each component offers the composer |
| `WorkspaceSuggestionWorkflow:MaximumSearchTermCountPerComponent` | `8` | How many words one component's search may carry |
| `WorkspaceSuggestionWorkflow:MaximumSearchTermCharacterCount` | `80` | How long one search term may be |

### 12.4 Catalogue tools

| Setting | Default | Meaning |
|---|---|---|
| `CatalogTools:McpEndpoint` | *(empty)* | The catalogue's MCP server. Absent ⇒ the agent cannot search |

### 12.5 Echo agent

| Setting | Default | Meaning |
|---|---|---|
| `EchoAgent:AgentName` | `echo-reverse-agent` | The name a console addresses it by |
| `EchoAgent:IsEnabled` | `true` | `false` stops serving it |

### 12.6 Environment variable convention

A setting is its section path with `__` for `:` — `ECHOAGENT__ISENABLED=false`, `WORKSPACESUGGESTIONWORKFLOW__MAXIMUMATTEMPTCOUNT=5`, `CATALOGTOOLS__MCPENDPOINT=https://host/mcp`. **Never define a setting of our own with a `FOUNDRY_` prefix** — the platform reserves it.

### 12.7 Precedence and local files

Settings are read from the environment first, then `appsettings.json`; a local `.env` fills anything the platform injected empty. **An empty setting is absent, not a value** — a missing required setting refuses startup by name. `.env.example` documents every key.

### 12.8 `azure.yaml`

```yaml
services:
    core-rental-workspace-suggestion-agent:
        project: src/CoreRentalNet.Agents
        host: azure.ai.agent
        name: core-rental-workspace-suggestion-agent   # what Foundry resolves
        codeConfiguration:
            entryPoint: CoreRentalNet.Agents.dll
        kind: hosted
        protocols:
            - protocol: responses
              version: 2.0.0
```

**There are two `azure.ai.agent` services, not one.** `azd` addresses *services* by their key, not the keyed agents inside a container, so `azd ai agent invoke echo-reverse-agent …` needs a service keyed `echo-reverse-agent`. Both point at the same project; the container serves both. `FOUNDRY_AGENT_NAME` is deliberately **not** mapped in `env:` — the platform injects it.

---

## 13. Testing & quality gates

### 13.1 The agent suite

```bash
cd agentfoundry
dotnet test AgentFoundry.sln
```

Current: **199 passed / 0 failed** (2026-10-12).

### 13.2 What each test proves

| Area | Covers |
|---|---|
| Graph | every stage runs in order; a rejection runs only the verifier; a rejected review returns to rephrasing; the third rejection ends `unavailable` |
| Stage contracts | the stage contracts over a fake chat client (`RephraserSpecTests`, `WorkspaceRequestVerificationContractTests`, `InvalidModelOutputTests`) |
| Contract schemas | every contract against its committed JSON Schema, both directions (`ContractSchemaTests`, `WorkspaceWorkflowContractSchemaTests`) |
| Guardrails | the pipeline, the ledger, the pool builder, the selection and vocabulary policies (`Guardrails/GuardrailTests`, `McpToolAnswerLedgerTests`, `WorkspaceComponentProductPoolBuilderTests`) |
| MCP token | the caller token is lifted, tools are offered only where declared, the guardrail fires (`CatalogueChatClientTests`, `McpAccessTokenHandlerTests`, `McpAccessTokenServiceTests`) |
| Host | settings, startup refusals, build identity (`AgentHostSettingsTests`, `StartupReportTests`, `AgentVersionTests`) |
| Echo | replies exactly, streams, builds from its roster (`EchoWorkflowTests`) |

### 13.3 Application-side tests are not in this suite

These tests measure the catalogue's search and live in the **application solution**, under `../tests/CoreRentalNet.IntegrationTests/`:

- `ExpandedSearchRecallTests` — each described component is findable by its terms, and none as one typed phrase.
- `NameSearchPoolCharacterisationTests` — the measurement the pool's width is derived from.
- `LiftiProductNameSearchServiceTests` — a term finds a description-only word; a typed search does not.

`dotnet test AgentFoundry.sln` does **not** run them; run them from the repository root against `CoreRentalNet.sln`.

### 13.4 Run it

```bash
cd agentfoundry/src/CoreRentalNet.Agents
cp .env.example .env
PORT=8088 dotnet run
```

### 13.5 Contract schemas

`shared/contracts/*.schema.json` holds the JSON Schema for every stage contract. A test checks each contract against its schema in both directions.

---

## 14. Build, versioning & CI

### 14.1 The solution

`AgentFoundry.sln` builds `src/CoreRentalNet.Agents` and `tests/CoreRentalNet.Agents.Tests`. It is separate from the application's `CoreRentalNet.sln`.

### 14.2 Version from the git tag

The build never reads Git. `../scripts/resolve-version.sh` turns the tag being built into a semantic version plus the short commit, and the pipeline passes it in.

### 14.3 The agent's remote build

Foundry builds the deployed agent **remotely** from a zipped project folder, so the repository root's `Directory.Build.props` is **not** in that build. The pipeline writes `../scripts/write-agent-version.sh`'s output — `Version.g.props` — inside `src/CoreRentalNet.Agents/`, and the csproj imports it when present. Each property becomes assembly metadata, so the running container reports the build it was made from. The file is gitignored, so a developer's build keeps the floor `0.1.0`.

### 14.4 The pipelines

- **`agentfoundry/azure-pipelines.yml`** — a **separate** Azure DevOps pipeline. Two stages: **CI** restores, builds and tests `agentfoundry/AgentFoundry.sln`; **Deploy** runs `azd provision` then `azd deploy`, after a manual approval. It is path-filtered to `agentfoundry/**`, `Directory.Build.props`, `global.json` and `scripts/**`. Configuration comes from the `core-rental-foundry` variable group; `azd` signs in with a federated token, so no secret is stored. Both agents deploy one at a time (naming the service), because the two services share one project directory.
- **The root `.github/workflows/ci.yml` does not run this suite** — it builds `CoreRentalNet.sln` only.

---

## 15. Deployment & operations

### 15.1 `azure.yaml` services

Two `azure.ai.agent` services, both pointing at `src/CoreRentalNet.Agents`, plus an `azure.ai.project` service that pins the existing Foundry project and its model deployments.

### 15.2 Deploy

The agent pipeline resolves the version, writes `Version.g.props`, runs `azd provision`, then `azd deploy` per service. Provision reconciles the declared model deployments into the project; deploy registers new agent versions.

### 15.3 Readiness and diagnostics

```bash
curl -s -o /dev/null -w '%{http_code}\n' http://127.0.0.1:8088/readiness      # 200
```

Startup prints, before anything else can fail, what a deployment must supply — each required setting as `set` or `MISSING` — the agent names, and the build identity read back from the artifact's own assembly metadata:

```text
[startup] host built; listening.
[startup] hosted agent deployable
[startup]   version                             : 1.4.1
[startup]   git sha                             : abc1234
[startup]   build id                            : 12345
[startup]   environment                         : production
[startup]   hosted                              : yes
[startup]   FOUNDRY_PROJECT_ENDPOINT            : set
[startup]   AZURE_AI_MODEL_DEPLOYMENT_NAME      : set
[startup]   ModelTransport                      : 00:10:00 per call, 2 transport retries
[startup]   FOUNDRY_AGENT_NAME                  : core-rental-workspace-suggestion-agent
[startup]   EchoAgent:AgentName                 : echo-reverse-agent (enabled)
[startup]   AgentHost:DefaultAgentName         : core-rental-workspace-suggestion-agent
```

Readiness checks a workflow agent for checkpointing: an agent that runs a workflow is checked that hosting can copy it. Register the workflow agent unwrapped, or readiness fails.

### 15.4 Serving several agents in one container

Hosting resolves a request by the `agent.name` it carries and looks the agent up in keyed services. Every feature registers its own keyed agent under its own name, so one container serves all of them. A request naming an unregistered agent falls back to the default and warns; a request naming **nothing** takes the default.

### 15.5 Known limitations

- **The caller's token is required to search.** A console invoke carries none, so it ends `unavailable` rather than composing from memory.
- **Locally, one container answers as one agent.** `azd ai agent invoke --local` sends no name, so the container replies with `AgentHost:DefaultAgentName`; a mismatch is silent.
- **`azd` resolves services, not agents.** A name you type after `azd ai agent invoke` must exist as a service key in `azure.yaml`.

### 15.6 Not yet decided

Scaling beyond one container, secret rotation, and any deployment of the agent outside azd are deliberate decisions not yet made.

---

## 16. Troubleshooting / FAQ

### 16.1 Local invoke answers the wrong agent

`azd ai agent invoke --local` sends no agent name and `azd ai agent run <service>` does not pass the service name through locally, so the container answers with `AgentHost:DefaultAgentName`. Start the service you intend to invoke, and check the startup line that says which agent a nameless request will get.

### 16.2 A console run cannot search the catalogue

The agent searches *as the caller*, and a console carries no caller token, so the run ends in an error at retrieval. Use the explicit request with the `x-client-mcp-catalog-access-token` header ([7.5](#75-invoke-the-workspace-agent-with-a-caller-token)).

### 16.3 Catalogue / MCP

- **The agent cannot search** → `CatalogTools:McpEndpoint` is empty, or the caller token lacks `read:catalog`.
- **Similarity search is not offered** → the `SimilaritySearch` claim is closed by default in the application.

### 16.4 Model / transport

`ModelTransport:NetworkTimeout` is how long one call may take; `ModelTransport:MaximumRetryAttempts` is how many times one failed call is retried. Neither is a run's attempt count — `WorkspaceSuggestionWorkflow:MaximumAttemptCount` is, and the two never share a counter.

### 16.5 Guardrails

- **A refused tool call is not an error** → the reason is returned to the model, which may correct the call; a throw would fail the stage.
- **A tool is refused before it runs** → an argument guard denied it; the chain stops.

---

## 17. Process & conventions

### 17.1 The authority chain

Read [`../CLAUDE.md`](../CLAUDE.md) first; it is the auto-loaded entry point and names the documents in the order they are read.

### 17.2 Hard rules

- **One commit per change** — a folder move and a behaviour change do not belong in the same commit.
- **Secrets.** The process holds no catalogue credential; never log the caller's token; keep `.env` out of version control.
- **Never define a setting with a `FOUNDRY_` prefix** — the platform reserves it.
- **Register the workflow agent unwrapped**, or readiness fails.

### 17.3 Commits & branches

The repository's convention is a revertible commit per step.

### 17.4 Where specs live

Output documents are written under `../specs/`. There is no `specs/adr/` directory; `CONVENTIONS.md` is the convention list those rules refer to.

---

## 18. Credits

### 18.1 Third-party

Built on the **Microsoft Agent Framework** (`Microsoft.Agents.AI.Foundry`, `Microsoft.Agents.AI.Foundry.Hosting`, `Microsoft.Agents.AI.Workflows`), **Azure AI Foundry** (`Azure.AI.Projects`, `Azure.Identity`), **ModelContextProtocol**, **DotNetEnv** (local `.env`), and **xUnit**. The application that owns the catalogue is a separate solution; see the [root README](../README.md).

### 18.2 References

- The whole system: [root `README.md`](../README.md).
- Conventions and tests: [`../CONVENTIONS.md`](../CONVENTIONS.md).
- Authority chain: [`../CLAUDE.md`](../CLAUDE.md).
