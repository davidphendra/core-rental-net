# CoreRentalNet.Agents

The hosted-agent deployable for CoreRentalNet. It serves one or more **Foundry hosted agents** over the
**Responses** protocol, and it is organised so that a feature — its instructions, its stages and its graph —
lives in one folder a newcomer can find without reading the whole project.

Two features ship today:

| Feature | Served agent | What it does | Cost |
|---|---|---|---|
| `Features/WorkspaceSuggestion` | `core-rental-workspace-suggestion-agent` | Turns one customer sentence into workspace setups drawn from the catalogue | A model call per stage, several per run |
| `Features/EchoReply` | `echo-agent` | Replies with the message it was sent | Nothing — no model, no catalogue |

---

## 1. Features

### 1.1 Workspace suggestion

One sentence in, workspace setups out. The customer's sentence, the slot rules and their own catalogue token
arrive in a single request; the agent searches the catalogue **as the caller**, composes a few genuinely
different setups, and streams what happens while it works.

**The graph.** A run is a bounded workflow, not a straight line:

```text
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
build candidate product pool      (deterministic: from the tool answers, per component) │
     ▼                                                           │
rerank per component              (best first, with a reason)     │
     ▼                                                           │
bound retrieved products          (deterministic: top-N, dedupe)  │
     ▼                                                           │
compose workspace setups                                         │
     ▼                                                           │
validate setup structure          (deterministic: provenance)     │
     │ structurally invalid ─────────────────────────────────────┤
     │ structurally valid                                        │
     ▼                                                           │
review workspace setups                                          │
     ├── acceptable ──► complete: success (setups streamed first) │
     └── rejected ────► decide retry ── attempts left ─────────────┘
                              └── attempts exhausted ──► complete: unavailable
```

Three rules the graph enforces, and the reason it is a graph rather than a prompt:

- **The verifier runs once.** It is on no retry edge, because whether a sentence is about a workspace does not
  change when a setup is rejected.
- **Retry returns to rephrasing, not composition.** A rejected setup set means the sentence has to be read
  *differently* — and, with the expansion below, searched differently; composing again from the same reading
  fails the same way.
- **No model decides the lifecycle.** The verifier and the reviewer return validity. Whether another attempt
  runs is deterministic code — `WorkspaceSetupRetryDecisionPolicy` — reading the verdict and the attempt count.

**The stages.** Five stage agents, each one prompt and one profile:

| Stage | Decides | Given | Tools |
|---|---|---|---|
| Verifier | Is this a workspace request at all? | the sentence, the slot rules | none |
| Rephraser | What does the customer want, and what words will find it? | the sentence, the slot rules, the currency, the ceiling the application recorded, the previous attempt's issues | none |
| Retriever | What does the catalogue offer? | one search per requested component: its words and the catalogue arguments that go with them | the catalogue's MCP tools |
| Reranker | Which of what it offered answers each component? | every requested component's need, and the products a search returned for it — names, descriptions and prices | none |
| Composer | Which products form a setup? | the expansion, the retrieved products | none |
| Reviewer | Does the setup set satisfy the request? | the sentence, the expansion, what each search came back with, the setups | none |

Four deterministic stages sit between them: `WorkspaceComponentProductPoolPolicy` (what the tools answered,
one product per SKU, at most fifteen per component), `WorkspaceComponentProductSelectionPolicy` (the reranker's
order, a SKU it was never given dropped), `WorkspaceSetupCandidateStructureValidator` (every SKU came from the
reranked set, quantities within capacity, components actually requested) and `WorkspaceSetupRetryDecisionPolicy`
(which of the three endings, and whether there is an attempt left to retry).
`WorkspaceComponentSearchVocabularyLimitPolicy` is applied **inside** the rephrasing stage
rather than as a stage of its own, because the words have to be bounded before the retriever can see them — and
`WorkspaceComponentProductSelectionPolicy` likewise inside reranking, where dropping an ungiven SKU and restoring
a product's slot are code rather than prompt.

**The pool is built from the tools' own answers, not from a model's report.** Every MCP tool answer is recorded as
it returns; the pool is read from those recordings, so a product's description reaches the reranker without any
model having retyped it. That is why the retriever no longer lists products at all — a model copying six hundred
characters can shorten or fuse them, and nothing downstream would notice.

**The expansion, and why the search had to change with it.** The rephraser answers with a *requirement
expansion*: what the whole workspace is for, what it may cost, and — for each of the seven catalogue components —
a retrieval query, search terms, synonyms and semantic concepts. That shape is the fix for what the pipeline
shipped with. A described need used to reach `search_catalogue` as a single phrase, and the name search requires
**every** word of what it is given, so a sentence about a need matched nothing at all. Measured over the real
205-product catalogue, seven needs of seven found **0** products as one typed phrase and **10 to 28** as terms.
The tool therefore takes **several terms that are alternatives**: every word of one term must appear, and a
product needs to match only one. `ExpandedSearchRecallTests` keeps both halves of that measurement.

**The index reads a product's description as well as its name, and only the terms may see it.** A need's
distinguishing words — *lumbar support*, *glare* — are stated in prose, so indexing only the name is what made
those needs unreachable. That widening is bounded to the expanded terms: what a customer types in a search box is
still answered from the name alone, so no search in the application answers differently from the day before it.
`LiftiProductNameSearchServiceTests` pins both halves — the word *lumbar* appears in three chairs' descriptions
and no chair's name, is found by a term, and is not found by the typed search.

**The budget acts on the search, not only on the composition.** When the customer stated a total and no
per-component split, the rephraser works out the allocation, and each component's `maximumMonthlyAmount` is
passed to the catalogue, which treats it as a **narrowing** filter: a product above it is not eligible, so a good
name match can never bring it back. A search that came back empty *because of that ceiling* reports the cheapest
product the ceiling excluded; the reviewer turns that figure into a correction, and the retry reallocates.
Without that path, a run whose allocation was too tight would report that no workspace fits a catalogue that
could have furnished one.

**The stream.** A caller does not receive one document. It receives a sequence of typed events, one JSON object
each, discriminated by `type`:

| `type` | Carries | When |
|---|---|---|
| `stageStarted` | `processingStage` | a stage begins |
| `stageCompleted` | `processingStage` | a stage finishes |
| `retry` | `nextAttemptNumber`, `maximumAttemptCount` | a rejected attempt gives way to another |
| `candidate` | `approvedWorkspaceSetup` | an approved setup — **after** review, never before |
| `completed` | `runStatus`, `outcomeReason`, `completedAttemptCount`, `runUsage` | the run ends |

`runStatus` is one of `success`, `rejected`, `unavailable`, and it is deliberately **separate** from the
candidates: a setup's arrival must never imply a successful run. No model text ever reaches a caller — only the
events a stage chooses to publish.

### 1.2 Echo

`echo-agent` answers with the caller's message, verbatim. Nothing else.

It exists so that hosting can be exercised from a console — routing by name, session handling, streaming — with
no model call, no catalogue and no cost. It is a **workflow with one stage** on purpose: hosting treats an agent
that runs a workflow differently from one that does not, so a plain agent would test a different path from the
one the real pipeline uses.

It is enabled by default and switched off in a deployment with `ECHOAGENT__ISENABLED=false`.

---

## 2. Architecture

### 2.1 Layout

```
src/CoreRentalNet.Agents/
├── Program.cs                     the composition root: reads as a list of features
├── appsettings.json
├── Shared/                        reused by every feature
│   ├── Agents/          AgentProfile · AgentFactory
│   ├── Configuration/   HostedAgentName
│   ├── Mcp/             caller-authorised MCP: header reader · per-call token holder ·
│   │                    connection · chat client · attachment handler · request policy
│   ├── Model/           model client · telemetry · guardrail · transport options · run usage
│   ├── Prompts/         embedded instruction source
│   ├── Serialization/   ContractJson
│   └── Workflows/       ChatEntryStageExecutor · StreamingStageExecutor · WorkflowOutputPublisher
└── Features/
    ├── WorkspaceSuggestion/       prompts · roster · domain · stages · policies · graph
    └── EchoReply/                 prompt · roster · stage client · stage · graph
```

**`Shared/` is anything a second feature would use unchanged.** Everything else belongs to a feature.

### 2.2 How an agent is defined

Every agent here is the same three artifacts, so adding one is a folder and a registration, not a new shape:

1. **A prompt** — `Features/<Feature>/Prompts/<name>.v<N>.md`, embedded into the assembly. The version lives in
   the file name, so a change is a new file and a run can name the instructions it used.
2. **A profile** — an `AgentProfile` in the feature's roster: stage name, prompt file, description, output
   format, and whether it may search.
3. **A stage** — an executor that calls its stage agent and publishes what came back.

`AgentFactory` builds the `AIAgent` from a profile and an `IChatClient`, which is what lets the whole tree run
offline against a fake.

### 2.3 Three constraints that shaped the code

Found by building against the pinned packages, not assumed:

- **The served agent must be the workflow agent itself, unwrapped.** Hosting redirects a hosted workflow's
  checkpoints only when it can copy that agent, and it declines to copy one behind middleware. So nothing wraps
  the served agent; per-call work lives one layer down, at each stage's model call.
- **A workflow served as an agent must speak the chat protocol.** `List<ChatMessage>` *and* a turn token, or
  hosting refuses the workflow before a run starts. That is what `ChatEntryStageExecutor` is for.
- **A workflow event does not cross the agent boundary as content; a yielded message does.** Every stage
  publishes through `WorkflowOutputPublisher`, which yields one assistant message per event.
- **A request that names no agent needs a default.** Hosting falls back to a non-keyed `AIAgent` when the
  request carries no name — and `azd ai agent invoke --local` sends no name at all. Each deployment therefore
  registers the agent it is named for as its default, so a nameless request gets the right one.

### 2.4 Cross-cutting concerns

| Concern | Where | Note |
|---|---|---|
| The caller's credential | `AccessTokenHeaderReader` → `IMcpAccessTokenService` → `McpAccessTokenHandler` | read once off the invocation, held for the call, stamped on each MCP request; never logged |
| Catalogue tools | the same decorator | offered only to a stage that declares `UsesCatalogueTools` |
| Run cost and model telemetry | `ModelCallTelemetryChatClient` | scoped to the run; the innermost decorator, wrapping the guardrail; one span and one line per call, the stage named by the call's options, and the run's total reported on the `completed` event |
| Token-leak guardrail | `ModelOutputGuardrailChatClient` | fails the run loudly if an answer ever repeats the caller's token |
| Transport retries | `ModelTransportRetryOptions` | **not** a run's attempt count; the two never share a counter |
| The `type` discriminator | `StreamingStageExecutor<TState, TEvent>` | the union is a class type parameter, so a stage cannot publish through a concrete type and silently drop `type` |

---

## 3. Configuration

Settings are read from the environment first, then `appsettings.json`; a local `.env` fills anything the
platform injected empty. **An empty setting is absent, not a value** — a missing required setting refuses
startup by name rather than serving an agent nothing can find.

### 3.1 Agent names

| Setting | Default | Meaning |
|---|---|---|
| `FOUNDRY_AGENT_NAME` | `core-rental-workspace-suggestion-agent` | **Reserved prefix.** The platform injects the deployed name; the value here is the local default. Must equal the `azure.yaml` service `name`. |
| `EchoAgent:AgentName` | `echo-agent` | The name a console addresses the echo agent by |
| `EchoAgent:IsEnabled` | `true` | Set `false` to stop serving the echo agent |
| `AgentHost:DefaultAgentName` | the workspace agent's name | Which agent a request that names none gets. **Set per service in `azure.yaml`** — see below |

**There are two `azure.ai.agent` services in `azure.yaml`, not one.** `azd` addresses *services* by their key in
that file — not the keyed agents inside a container — so `azd ai agent invoke echo-agent …` needs a service
keyed `echo-agent`. Both services point at the same project; the container runs the same code either way and
serves both agents, and the service `name:` is what Foundry resolves and what the platform injects as
`FOUNDRY_AGENT_NAME`. That injected name is also the container's **default agent**, so a request that names
nothing gets the agent the deployment is named for.

### 3.2 Model and transport

| Setting | Default | Meaning |
|---|---|---|
| `FOUNDRY_PROJECT_ENDPOINT` | *(empty)* | The Foundry project. Required; injected by the platform in a container |
| `AZURE_AI_MODEL_DEPLOYMENT_NAME` | `gpt-4.1-mini` | The deployment every stage runs on |
| `ModelTransport:NetworkTimeout` | `00:10:00` | How long one model call may take before the transport gives up |
| `ModelTransport:MaximumRetryAttempts` | `2` | How many times one failed call is retried |

### 3.3 Workspace suggestion

| Setting | Default | Meaning |
|---|---|---|
| `WorkspaceSuggestionWorkflow:MaximumAttemptCount` | `3` | How many times a run may rephrase and try again |
| `WorkspaceSuggestionWorkflow:MaximumRetrievedProductsPerComponentForReranking` | `15` | How many products each component offers the reranker |
| `WorkspaceSuggestionWorkflow:MaximumSearchTermCountPerComponent` | `8` | How many words one component's search may carry, terms and synonyms together |
| `WorkspaceSuggestionWorkflow:MaximumSearchTermCharacterCount` | `80` | How long one search term may be |
| `WorkspaceSuggestionWorkflow:MaximumSelectedProductsPerComponent` | `3` | How many products each component offers the composer |
| `CatalogTools:McpEndpoint` | *(empty)* | The catalogue's MCP server. Absent ⇒ the agent cannot search rather than answering from memory |

The catalogue's two tools are gated by the **caller's** token, in the application's configuration:
`search_catalogue` needs `read:catalog`, which ships enabled; `search_similarity_catalogue` needs the
`SimilaritySearch` claim, which ships **closed** and is set to `poweruser:aibuilder` in local development. The
retriever is offered whichever ones the caller's token entitles, which is why the process configures no
credential of its own — and why the same run can discover one tool or two.

### 3.4 Environment variables

A setting is its section path with `__` for `:` — `ECHOAGENT__ISENABLED=false`,
`WORKSPACESUGGESTIONWORKFLOW__MAXIMUMATTEMPTCOUNT=5`, `CATALOGTOOLS__MCPENDPOINT=https://host/mcp`.
`.env.example` documents every one. **Never define a setting of our own with a `FOUNDRY_` prefix** — the
platform reserves it.

### 3.5 `azure.yaml`

```yaml
services:
    workspace-suggestions:
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

`FOUNDRY_AGENT_NAME` is deliberately **not** mapped in `env:` — the platform injects it, and mapping it from an
azd variable nobody set once injected it empty.

---

## 4. Testing

### 4.1 Unit and contract tests

```bash
cd agentfoundry
dotnet test AgentFoundry.sln
```

| Suite | Covers |
|---|---|
| `WorkflowTests` | the graph: every stage runs in order, a rejection runs only the verifier, a rejected review returns to rephrasing, the third rejection ends `unavailable` |
| `ExpandedSearchRecallTests` | the measurement: each described component is findable by its terms, and none is findable as one typed phrase |
| `NameSearchPoolCharacterisationTests` | the measurement the pool's width is derived from: the terms rule puts the product within fifteen, a single broad term buries it at #14–#30 |
| `McpToolAnswerLedgerTests` · `WorkspaceComponentProductPoolBuilderTests` · `WorkspaceComponentProductSelectionPolicyTests` | the recorded answers, and the two guarantees a reranker's answer is read under |
| `WorkflowGraphSpikeTests` | the two hosting mechanics the design rests on: the chat-protocol requirement and yielded output reaching the caller |
| `WorkspaceWorkflowContractSchemaTests` · `ContractSchemaTests` | every contract against its committed JSON Schema, in both directions |
| `RephraserSpecTests` · `WorkspaceRequestVerificationContractTests` · `InvalidModelOutputTests` | the stage contracts over a fake chat client |
| `EchoReplyChatClientTests` · `EchoReplyWorkflowTests` | the echo replies exactly, streams, and builds from its roster |
| `WorkspaceSuggestionAgentIdentityTests` · `EchoAgentIdentityTests` | configured names, and startup refusing a missing one by name |
| `CatalogueChatClientTests` · `ModelCallDecoratorTests` | the caller token is lifted, tools are offered only where declared, the guardrail fires |

### 4.2 Run it locally

```bash
cd agentfoundry/src/CoreRentalNet.Agents
cp .env.example .env          # then fill FOUNDRY_PROJECT_ENDPOINT and AZURE_AI_MODEL_DEPLOYMENT_NAME
PORT=8088 dotnet run
```

or, through azd — **naming the service you intend to invoke**:

```bash
cd agentfoundry
azd ai agent run workspace-suggestions     # this container's default agent: the workspace agent
azd ai agent run echo-agent                # this container's default agent: the echo agent
```

The host binds `PORT` (default `8088`) and **ignores** `ASPNETCORE_URLS`.

> **A `dotnet run` container answers as the workspace agent.** With no `azure.ai.agent` service behind it, the
default falls back to `FOUNDRY_AGENT_NAME` — the workspace agent — and `azd ai agent invoke --local` sends no
agent name, so `azd ai agent invoke echo-agent "…" --local` against that container is answered by the
**workspace** agent. Start the container as the service you mean to invoke, and check it before invoking: every
startup prints which agent a nameless request will get.
>
> ```text
> [startup]   AgentHost:DefaultAgentName        : echo-agent
> ```

### 4.3 Check it is ready

```bash
curl -s -o /dev/null -w '%{http_code}\n' http://127.0.0.1:8088/readiness      # 200
```

Startup prints one line per setting and one per served agent, which is what a container that is never
acknowledged needs:

```text
[startup]   FOUNDRY_AGENT_NAME                : core-rental-workspace-suggestion-agent
[startup]   EchoAgent:AgentName               : echo-agent (enabled)
[startup] serving core-rental-workspace-suggestion-agent
[startup] serving echo-agent
```

### 4.4 Invoke the echo agent — the smoke test

```bash
azd ai agent run echo-agent                              # terminal 1 — note the service name
azd ai agent invoke echo-agent "hello there" --local    # terminal 2
azd ai agent invoke echo-agent "hello there"            # against the deployment
```

Expected: `hello there`, in a fraction of a second and with no model call.

**The container you run must be the service you invoke.** `azd ai agent invoke --local` sends a request with
no agent name at all, and `azd ai agent run <service>` does not pass the service name through locally — only the
hosting platform injects `FOUNDRY_AGENT_NAME`. So a local container can only answer with the one agent it was
started as, which is why each `azure.ai.agent` service sets `AGENTHOST__DEFAULTAGENTNAME` in `azure.yaml`.
Running the echo container and then invoking `workspace-suggestions` against it will answer with the echo, and
nothing can detect the mismatch, because azd never sends the name. Then, without any
client:
```bash
curl -sS -N -H 'Content-Type: application/json' -H 'Accept: text/event-stream' \
  -d '{"model":"echo-agent","input":"hello there","stream":true}' \
  http://127.0.0.1:8088/responses
```

### 4.5 Invoke the workspace agent

From a console, a sentence is enough:

```bash
azd ai agent run workspace-suggestions
azd ai agent invoke workspace-suggestions "a desk and a chair under 500000" --local
```

A console sends a sentence, not the application's envelope, so the run reads it as a query over every slot this
deployment can compose for, at capacity one and with no ceiling.

**A console cannot search the catalogue.** The agent searches *as the caller*, and a console invoke carries no
caller token, so a run from the command line ends in an error at the retrieval stage rather than composing from
memory. That is the trust model, not a defect: use the full request below, with a token, to exercise the
pipeline end to end.

With an explicit request, which is the shape that carries the token, the ceiling and the capacities:

```bash
curl -sS -N -H 'Content-Type: application/json' -H 'Accept: text/event-stream' \
  -H 'x-client-mcp-catalog-access-token: <the caller's token>' \
  -d '{"model":"core-rental-workspace-suggestion-agent","input":"{\"runId\":\"r1\",\"query\":\"a desk and a chair under 500000\",\"currency\":\"IDR\",\"ceilingMonthly\":500000,\"slots\":[{\"slot\":\"Desk\",\"capacity\":1},{\"slot\":\"Chair\",\"capacity\":1}]}","stream":true}' \
  http://127.0.0.1:8088/responses
```

A run streams one typed event per line, in order:

```text
{"type":"stageStarted","customerWorkflowIdentifier":"r1","processingStage":"verifyingRequest"}
…
{"type":"retry",   "nextAttemptNumber":2,"maximumAttemptCount":3}
{"type":"candidate","approvedWorkspaceSetup":{"lines":[…],"rationale":"…"}}
{"type":"completed","runStatus":"success","completedAttemptCount":2,
 "runUsage":{"modelCalls":11,"inputTokens":9541,"outputTokens":844,"model":"gpt-4.1-mini","promptVersion":"…"}}
```

The token is required, and it travels on the invocation rather than in the body: the agent searches the catalogue
**as the caller**, so a run whose request carries no `x-client-mcp-catalog-access-token` header reports
`unavailable` rather than composing from memory.

### 4.6 End-to-end against the real catalogue

The catalogue lives in the application repository:

1. Start the application host (it publishes the catalogue's MCP endpoint at `/mcp`).
2. Mint a caller token that carries `read:catalog`.
3. Point the agent at it: `CATALOGTOOLS__MCPENDPOINT=<host>/mcp`.
4. Invoke as in 4.5 and confirm real SKUs, names and amounts in the `candidate` events.

---

## 5. Operational notes

- **Serving several agents.** Hosting resolves a request by the `agent.name` it carries and looks the agent up
  in keyed services. Every feature registers its own keyed agent under its own name, so one container serves
  all of them. A request naming an unregistered agent falls back to the default and warns; a request naming
  **nothing** takes the default, which is the agent `FOUNDRY_AGENT_NAME` names.
- **`azd` resolves services, not agents.** A name you want to type after `azd ai agent invoke` has to exist as
  an `azure.ai.agent` service key in `azure.yaml`. That is why the echo agent has its own service entry even
  though it shares a container.
- **Locally, one container answers as one agent.** `azd ai agent invoke --local` sends no agent name, so the
  container replies with `AgentHost:DefaultAgentName`. Run the service you intend to invoke; a mismatch is
  silent, because there is nothing in the local request for the container to check the name against.
- **Readiness checks a workflow agent for checkpointing.** An agent that runs a workflow is checked that
  hosting can copy it; anything else is passed over. Register the workflow agent unwrapped, or readiness fails.
- **Secrets.** The process holds no catalogue credential: the caller's token arrives in the request and is
  lifted out before the model sees it. Never log it, and keep `.env` out of version control.
- **One commit per change.** The repository's convention is a revertible commit per step; a folder move and a
  behaviour change do not belong in the same one.
