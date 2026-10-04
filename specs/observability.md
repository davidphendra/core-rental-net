# Observability for CoreRentalNet.Agents

The requirement in flight for the hosted-agent deployable at `agentfoundry/`. It records the decisions
taken, the telemetry vocabulary, the cross-check against established observability practice, the
dashboards and alerts, and the delivery plan.

Read with `agentfoundry/README.md` (the module map and the constraints that shaped the code) and
`CONVENTIONS.md` (the code and test budgets). Where this document names a setting, a metric or a span, the
name is normative: it is the vocabulary the plan's tests pin.

**Status:** decided, not yet built. Nothing in this document has been implemented.

---

## 0. Decision register

Product decisions (the original five):

| # | Decision | Choice |
|---|---|---|
| A1 | Observability backend | **Azure Monitor / Application Insights** |
| A2 | Message-content capture | **On everywhere, for now** (revisited after the baseline — F6) |
| A3 | OpenTelemetry Collector | **None anywhere** |
| A4 | Run-level span | **Our own, child of the platform's server span** |
| A5 | Release scope | **Business layer first, framework layer second** |

Confirmations taken during review:

| # | Decision | Choice |
|---|---|---|
| C4 | Package references | **None new**; reuse the transitive OTel graph; S1 confirms compile-visibility, else fall back |
| C5 | `TelemetryChatClient`'s custom span | **Remove** it in S7; keep the run-cost totals |
| C8 | Our business spans | **Content-free**, enforced by a test |
| D1 | Metric catalog scope | **Full catalog** |
| D2 | Failure taxonomy | **Split**: terminal `run.failure_reason` and attempt-level `retry.reason` |
| D3 | Review issue codes | **Tag by a bounded mapping** (7 prompt codes + `other`) |
| D6 | Groundedness metric | **One reason-tagged validator counter**; groundedness is the `sku_not_retrieved` series |
| D7 | Retrieval effectiveness | **Measured at search level**, not term level; the term-level recall claim stays a test |
| D8 | Dimensions | **Bounded set with placement discipline**; `run.prompt.version` on run-level metrics only |
| E1 | Dashboard container | **Two separate Workbooks** (technical and business), cross-linked |
| E2 | Board roles | **Distinct**; technical answers "where", business answers "what" |
| E3 | Latency distribution | **p50 / p95 / p99** on every duration panel |
| E4 | Exemplars | **On every duration panel**, with a slow-traces table as the S1 fallback |
| E5 | Sampling | **Metrics unsampled (safety floor); spans sampled** |
| E6 | Business-board content | **Content drill-down allowed**, gated by the same RBAC as the technical board |
| E8 | Foundry evaluator metrics | **Included in this release** |
| E8b | Evaluator mode | **Continuous evaluation on sampled live traces** |
| F4 | Thresholds | **Baseline first**; only zero-expected alerts ship immediately |
| F5 | Instrument lifecycle | **Delete rule adopted** |
| F6 | Content capture | **Decided at the end of the baseline**, on measured bytes and redaction count |
| F8 | Telemetry tests | **BCL `ActivityListener` / `MeterListener`**, no new package |
| DC1–DC6 | Dashboard corrections | Tokens by stage; review-issues panel; p95 on the time-to-value tile; per-tool availability; `slot` in the parameter bar; node success/fail panel |
| DC7 | `rerank.duration` | **Not emitted**; covered by `node.duration{node=rerank}` |

---

## 1. What already exists

Everything below was read from the pinned packages and Microsoft's own documentation; **S1 confirms it by
running it**, because the repo rule is "do not assume".

| Fact | Evidence | Status |
|---|---|---|
| `AgentHost.CreateBuilder` wires OpenTelemetry | `Program.cs` comment; `AddAgentHostTelemetry` → `UseMicrosoftOpenTelemetry` in `Azure.AI.AgentServer.Core` | observed, confirm in S1 |
| The Microsoft OpenTelemetry distro (`Microsoft.OpenTelemetry` 1.0.0-beta.1) is present and compile-available | `agentfoundry/src/CoreRentalNet.Agents/obj/project.assets.json` | observed |
| The distro selects exporters from the environment: Azure Monitor, Agent365, OTLP, Console | `Microsoft.OpenTelemetry.xml` → `MicrosoftOpenTelemetryOptions.Exporters` | observed |
| The distro auto-instruments ASP.NET Core, HttpClient, SqlClient, Azure SDK, OpenAI, Semantic Kernel and Agent Framework (opt-out, default on) | `Microsoft.OpenTelemetry.InstrumentationOptions` | observed |
| `FoundryEnvironment` reads `AppInsightsConnectionString`, `OtlpEndpoint`, `IsAppInsightsEntraAuth`, `IsAgent365TracingEnabled` | reflection over `Azure.AI.AgentServer.Core.dll` | observed |
| Agent Framework emits GenAI-conformant traces, metrics and logs | MAF Observability documentation | observed |
| Default framework source names are `Experimental.Microsoft.Agents.AI` and `Experimental.Microsoft.Extensions.AI` | `Microsoft.Agents.AI.OpenTelemetryConsts`, `Microsoft.Extensions.AI.OpenTelemetryConsts` | observed |
| An active span is propagated to MCP servers (`params._meta`) automatically | MAF Observability documentation | observed |
| Every span is enriched with Foundry agent identity and project | `FoundryEnrichmentProcessor`, `AgentServer.Core.xml` | observed |
| The only instrumentation in our code today is an unregistered `stage-model-call` span | `Shared/ChatClients/TelemetryChatClient.cs` | observed |

**No new package reference is required** (decision C4). `Microsoft.OpenTelemetry`, `OpenTelemetry`,
`OpenTelemetry.Exporter.OpenTelemetryProtocol`, `Microsoft.Extensions.AI` and `Microsoft.Agents.AI` are all
transitive compile assets. This matters because this project is zipped and built server-side
(`ImportDirectoryPackagesProps=false`), so every added reference risks a version conflict with the pinned
preview set.

### 1.1 The constraint that shapes the instrumentation

`agentfoundry/README.md` §2.3: **the served agent must be the workflow agent itself, unwrapped.**
Hosting redirects a hosted workflow's checkpoints only when it can copy the agent, and it declines to copy
one behind middleware. Therefore:

- Framework OpenTelemetry **must not** wrap the served workflow agent (`IWorkspaceSuggestionWorkflow.AsAIAgent()`).
- It goes on the **per-stage agents** (`WorkspaceSuggestionAgentBuilder.For`) and the **shared chat
  client** (`ITelemetryChatClient`), which is exactly where the README says per-call work belongs.

---

## 2. Instrumentation architecture

Three layers, one per concern:

1. **Business layer (ours)** — one `ActivitySource` and one `Meter`, named `CoreRentalNet.Agents`, carrying
   the workflow semantics the framework cannot know: run lifecycle, node timing, retries, retrieval
   yield, candidate counts, budget behaviour, outcomes.
2. **Framework layer (adopted, decision A5)** — Agent Framework and `Microsoft.Extensions.AI`
   `UseOpenTelemetry` on the stage agents and the shared chat client: standard `invoke_agent`, `chat` and
   `execute_tool` spans, GenAI token/latency metrics, MCP trace propagation, and the content capture
   decision A2 requires.
3. **Host layer (adopted)** — the platform distro owns provider construction, sampling, resource
   detection, Foundry enrichment and export. We subscribe our source and meter to it.

`TelemetryChatClient` keeps its job of totalling the run's cost for `AgentRunUsage`; its bespoke
`stage-model-call` span is removed once the framework layer lands (decision C5), because the framework's
`chat` span supersedes it. No test asserts that span; the stage-attribution tests assert the log line,
which stays.

A fourth signal source is added by decision E8/E8b: **Foundry continuous evaluation** scores a sample of
live traces and emits the quality metrics in §4.2.

### 2.1 The trace tree

```
POST /responses                                     [ASP.NET Core, platform]
└─ workspace.suggestion.run                          [CoreRentalNet.Agents] kind=Internal
   ├─ <executor id>                                  [CoreRentalNet.Agents] one per node
   │   └─ invoke_agent <stage agent name>            [MAF] gen_ai.agent.name
   │       ├─ chat <model>                           [M.E.AI] gen_ai.usage.*
   │       └─ execute_tool <tool name>               [M.E.AI]
   │            └─ (catalogue MCP server span, joined via params._meta)
   └─ complete-workspace-suggestion-<outcome>
```

The single bracket for node spans is `WorkspaceSuggestionStreamingExecutor.HandleAsync` — the class
comment already states it is *the one place every node is bracketed*. The entry node
(`WorkspaceInputExecutor`) writes its own line and starts the run span. The three completion executors
close it: `WorkspaceSuggestionSuccessCompletionExecutor`, `WorkspaceSuggestionRejectionCompletionExecutor`,
`WorkspaceSuggestionUnavailableCompletionExecutor`.

---

## 3. Telemetry vocabulary

### 3.1 Spans

| Span | Source | Kind | Carries | Where |
|---|---|---|---|---|
| `workspace.suggestion.run` | `CoreRentalNet.Agents` | Internal | `run.id`, `run.currency`, `run.ceiling.present`, `run.slot.count`, `run.capacity.total`, `run.status`, `run.outcome`, `run.attempts`, `run.model`, `run.prompt.version`, `run.model.calls`, `run.input.tokens`, `run.output.tokens` | input node starts; completion executors close |
| executor id (e.g. `verify-workspace-request`) | `CoreRentalNet.Agents` | Internal | `node.kind` = deterministic/model, `run.attempt` | `WorkspaceSuggestionStreamingExecutor.HandleAsync` |
| framework spans | `Experimental.Microsoft.Agents.AI`, `Experimental.Microsoft.Extensions.AI` | — | GenAI semantic conventions | framework, adopted |

`run.id` (the customer workflow identifier) is a **span attribute only** — never a metric tag. Our spans
carry **no content** (decision C8): the sentence, prompts and model text live only on the framework spans.

### 3.2 Metrics

Meter: `CoreRentalNet.Agents`. Source of each row is **ours** (an instrument we create), **framework**
(the distro/MAF emits it), **eval** (Foundry continuous evaluation), or **derived** (computed in App
Insights, no instrument).

**Tier** is the evidence grade from §4.3: **1** = established external practice, **2** = grounded in this
repository's documented facts, **3** = hypothesis to validate in the baseline period.

#### Run and outcome

| Metric | Type | Unit | Tags | Tier | Source |
|---|---|---|---|---|---|
| `workspace.run.count` | Counter | `{run}` | `run.status`, `run.prompt.version` | 1 | ours |
| *(run duration)* | — | — | — | 1 | **derived** from the `workspace.suggestion.run` span's duration, which cannot drift from the span |
| `workspace.run.attempts` | Histogram | `{attempt}` | `run.status` | 2 | ours |
| `workspace.run.failure_reason.count` | Counter | `{run}` | `reason` (terminal, bounded) | 2 | ours |
| `workspace.run.stuck.count` | Counter | `{run}` | — | 3 | ours |
| `workspace.guardrail.token_leak.count` | Counter | `{incident}` | `stage` | 2 | ours |
| `workspace.funnel.stage.count` | Counter | `{run}` | `stage`, `reached` | 1 | ours |

Terminal `reason` values (decision D2): `not_a_workspace_request`, `attempts_exhausted`,
`catalogue_unreachable`, `caller_token_missing`, `model_error`, `guardrail_token_leak`.

#### Retry

| Metric | Type | Unit | Tags | Tier | Source |
|---|---|---|---|---|---|
| `workspace.retry.count` | Counter | `{retry}` | `reason` (attempt-level, bounded) | 2 | ours |
| `workspace.retry.outcome.count` | Counter | `{retry}` | `attempt.number`, `result` | 2 | ours |

Attempt-level `reason` values (decision D2): `structure_invalid`, `no_products_found`, `budget_too_tight`,
`review_rejected`. These do **not** end a run; keeping them out of `run.failure_reason` is what makes that
metric mean "how the run ended".

#### Nodes

| Metric | Type | Unit | Tags | Tier | Source |
|---|---|---|---|---|---|
| `workspace.node.duration` | Histogram | `ms` | `node`, `node.kind` | 1 | ours |
| `workspace.node.count` | Counter | `{node}` | `node`, `status` | 1 | ours |

#### Model

| Metric | Type | Unit | Tags | Tier | Source |
|---|---|---|---|---|---|
| `gen_ai.client.operation.duration` | Histogram | `s` | `gen_ai.agent.name`, `gen_ai.request.model` | 1 | framework |
| `gen_ai.client.token.usage` | Histogram | `{token}` | `gen_ai.token.type`, `gen_ai.agent.name` | 1 | framework |
| `workspace.model.time_to_first_token` | Histogram | `ms` | `stage` | 1 | ours |
| `workspace.model.time_per_output_token` | Histogram | `ms` | `stage` | 1/3 | ours |
| `workspace.model.contract_violation.count` | Counter | `{violation}` | `stage` | 2 | ours |
| `workspace.model.throttled.count` | Counter | `{response}` | `model` | 3 | ours |
| `workspace.model.transport_retry.count` | Counter | `{retry}` | `model` | 2 | ours |
| `workspace.model.timeout.count` | Counter | `{timeout}` | — | 2 | ours |
| `workspace.model.error.count` | Counter | `{error}` | `error.type` (bounded) | 1/2 | ours |
| `workspace.model.tokens_per_minute` | Gauge | `{token}/min` | `model` | 3 | derived |

#### Catalogue and retrieval

| Metric | Type | Unit | Tags | Tier | Source |
|---|---|---|---|---|---|
| `workspace.catalogue.search.count` | Counter | `{search}` | `tool.name`, `outcome` | 1 | ours |
| `workspace.catalogue.products.retrieved` | Histogram | `{product}` | `component`, `tool.name` | 2 | ours |
| `workspace.catalogue.zero_result.count` | Counter | `{search}` | `component`, `tool.name`, `reason` = catalogue/budget | 2 | ours |
| `workspace.catalogue.excluded_by_ceiling.count` | Counter | `{product}` | `component` | 2 | ours |
| `workspace.catalogue.distinct_products.count` | Histogram | `{product}` | — | 3 | ours |
| `workspace.catalogue.connection.duration` | Histogram | `ms` | `tool.name` | 2 | ours |
| `workspace.catalogue.connection.failure.count` | Counter | `{failure}` | `tool.name` | 2 | ours |
| `workspace.catalogue.tool.timeout.count` | Counter | `{timeout}` | `tool.name` | 2 | ours |
| `workspace.catalogue.tool_available` | Gauge | `1` | `tool.name` | 2 | ours |
| `workspace.catalogue.auth_failure.count` | Counter | `{failure}` | `reason` = missing/expired | 2 | ours |
| `workspace.catalogue.untrusted_data.count` | Counter | `{incident}` | `tool.name` | 2 | ours (guardrail) |

Retrieval effectiveness is measured at **search level** (decision D7), because `CatalogueSearchToolAnswer`
carries product rows and `cheapestProductIgnoringTheCeiling` but no per-term attribution. The headline
"search hit ratio" is **derived**: `1 − zero_result / search.count`. The README's term-level recall claim
(0 products as one phrase, 10–28 as terms) stays a **test** (`ExpandedSearchRecallTests`), not a runtime
metric.

#### Composition, review and budget

| Metric | Type | Unit | Tags | Tier | Source |
|---|---|---|---|---|---|
| `workspace.rerank.relevance.count` | Counter | `{product}` | `level` = high/medium/low | 3 | ours |
| `workspace.candidate.count` | Histogram | `{setup}` | `outcome` | 2 | ours |
| `workspace.setup.approved.count` | Counter | `{setup}` | — | 2 | ours |
| `workspace.setup.monthly_total` | Histogram | `{currency}` | `currency` | 2 | ours |
| `workspace.setup.lines` | Histogram | `{line}` | — | 3 | ours |
| `workspace.setup.distinct_sku` | Histogram | `{sku}` | — | 3 | ours |
| `workspace.setup.component_coverage` | Histogram | `{component}` | — | 3 | ours |
| `workspace.validator.rejection.count` | Counter | `{rejection}` | `reason` (bounded) | 2 | ours |
| `workspace.review.issues.count` | Histogram | `{issue}` | — | 2 | ours |
| `workspace.review.issue.count` | Counter | `{issue}` | `issue.code` (bounded) | 2 | ours |
| `workspace.budget.headroom` | Histogram | `{currency}` | `currency` | 2 | ours |
| `workspace.budget.derived.count` | Counter | `{budget}` | — | 2 | ours |
| `workspace.budget.explicit.count` | Counter | `{budget}` | — | 2 | ours |
| `workspace.request.component_filled_ratio` | Histogram | `1` | — | 2 | ours |
| `workspace.component.shortfall.count` | Counter | `{component}` | `component` | 2 | ours |

Validator `reason` values are the violation prefixes the validator already produces:
`setup_has_no_lines`, `slot_not_requested`, `sku_not_retrieved`, `quantity_above_capacity`. Groundedness is
the `sku_not_retrieved` series (decision D6) — there is no separate provenance instrument. The violation
strings embed a slot or a **SKU**, so only the prefix is ever a tag.

`review.issue.count{issue.code}` uses the seven prompt-enumerated codes plus `other` (decision D3):
`MISSING_SLOT`, `SLOT_PURPOSE_UNSATISFIED`, `MONTHLY_CEILING_EXCEEDED`, `SETUPS_NOT_DISTINCT`,
`QUERY_MISMATCH`, `COMPONENT_BUDGET_EXCLUDES_ALL`, `BUDGET_ALLOCATION_EXCEEDS_TOTAL`. A code the model
invents collapses to `other`, so cardinality stays at eight.

#### Quality (Foundry continuous evaluation)

| Metric | Type | Tags | Tier | Source |
|---|---|---|---|---|
| `workspace.eval.groundedness` | Histogram | `stage` | 1 | eval |
| `workspace.eval.relevance` | Histogram | `stage` | 1 | eval |
| `workspace.eval.tool_call_accuracy` | Histogram | `stage` | 1 | eval |
| `workspace.eval.task_completion` | Histogram | `stage` | 1 | eval |

Continuous evaluation scores a **sample** of live traces. Evaluators call a model, so this carries its own
token cost and sample rate, separate from the telemetry sampling in decision E5.

#### Cost, stream and self-observation

| Metric | Type | Unit | Tags | Tier | Source |
|---|---|---|---|---|---|
| `workspace.cost.estimated` | Histogram | `{currency}` | `model`, `run.status` | 2 | derived from token usage |
| `workspace.cost.per_stage` | Histogram | `{currency}` | `stage` | 2 | derived |
| `workspace.stream.first_event` | Histogram | `ms` | — | 1/3 | ours |
| `workspace.stream.gap` | Histogram | `ms` | — | 3 | ours |
| `workspace.stream.abandoned.count` | Counter | `{run}` | `stage` | 3 | ours |
| `workspace.runs.in_flight` | UpDownCounter | `{run}` | — | 1 | ours |
| `workspace.workflow.checkpoint_resume.count` | Counter | `{resume}` | `result` | 3 | ours |
| `workspace.telemetry.redaction.count` | Counter | `{span}` | `reason` | 2 | ours |
| `workspace.telemetry.exporter_failure.count` | Counter | `{failure}` | — | 1 | derived from OTel self-telemetry |

### 3.3 Metrics the caller's list named that the framework already emits

Do not re-emit these. Map the original request onto what the framework provides:

| Requested | Provided by |
|---|---|
| `llm.request.duration` | `gen_ai.client.operation.duration` (framework) |
| `llm.input.tokens` / `llm.output.tokens` | `gen_ai.client.token.usage` with `gen_ai.token.type` (framework) |
| `llm.errors` | framework span status + `workspace.model.error.count` |
| `tool.calls` / `tool.duration` / `tool.errors` | `execute_tool` spans; ours adds the catalogue-specific metrics |
| `agent.execution.duration` / `.count` | `invoke_agent` spans (framework) |
| `agent.requests` / `.request.duration` | `workspace.run.count` / `.duration` (ours) |

### 3.4 Dimensions

Added to the metrics above where the value is bounded, with **placement discipline** (decision D8): a
dimension appears on the metric that needs it and nowhere else, and `run.prompt.version` only on
run-level metrics.

`run.status`, `run.prompt.version`, `stage`, `node`, `node.kind`, `model`, `tool.name`, `component`
(7 values), `slot` (7 values), `attempt.number`, `budget.explicit`, `tool.entitlement.similarity`,
`error.type`, `reason`, `level`.

### 3.5 The cardinality rule

Never a metric tag: `run.id` (customer workflow identifier), the customer's sentence, SKU, product name,
tenant, caller identity, the caller's token, or a raw exception message. Two values in this codebase look
like reasons and are not: `WorkspaceSetupReviewIssue.IssueCode` (bounded by mapping — D3) and the
validator's violation strings (bounded by prefix — D6).

Span attributes are exempt: a per-trace value such as `run.id` belongs on the run span.

### 3.6 Logs

`ILogger` already exists in every node and carries the node's result. Keep the existing lines; add
`run.id` as a log scope so traces, metrics and logs correlate. The token-leak guarantee extends to
telemetry: a test asserts no log line and no span attribute contains the caller's token.

---

## 4. Cross-check against established practice

Each framework below is a checklist from external practice. "Covered" means the inventory in §3 already
satisfies it; "gap" means a new instrument was added to satisfy it; "rejected" means it does not apply.

### 4.1 Technical standards

| Standard | What it mandates | Cross-check result |
|---|---|---|
| **Google SRE golden signals** (latency, traffic, errors, saturation) | Latency incl. tail; traffic rate; errors; resource saturation | Latency covered by `run.duration`, `node.duration`, `model.time_to_first_token`; tail is a dashboard requirement (E3). Traffic covered by `run.count`. Errors covered by `run.failure_reason`, `model.error`, `contract_violation`. Saturation → `model.tokens_per_minute` |
| **RED** (rate, errors, duration) per dependency | Each dependency needs its own rate/errors/duration | Model covered by framework + `model.error`. Catalogue covered by `connection.duration`/`failure`. Gaps closed: `catalogue.tool.timeout.count`, `model.timeout.count`, `model.error{error.type}` |
| **USE** (utilization, saturation, errors) | Resource utilisation and queueing | Process resources come free from the distro. Gaps closed: `model.tokens_per_minute`, `runs.in_flight` |
| **Little's Law / queueing** | Relate arrivals, in-flight work and wait time | `runs.in_flight` + `run.count` suffice; an explicit wait-time instrument is **rejected** — there is no queue we control |
| **OTel GenAI semantic conventions** | `gen_ai.client.operation.duration`, `gen_ai.client.token.usage`, first-token and per-token latency, tool/agent spans | Framework covers client metrics and spans. Gaps closed: `model.time_to_first_token`, `model.time_per_output_token` |
| **LLM-serving practice** | Time-to-first-token, output throughput, cost per token | Gaps closed: `model.time_per_output_token`, `cost.per_stage` |
| **FinOps unit economics** | Cost per unit that matters; allocate to the cost driver | `cost.estimated` + `cost.per_stage`; cost per approved setup is **derived** |
| **OTel self-observability** | Detect dropped spans and failed metric export | Gap closed: `telemetry.exporter_failure.count`; the sampling-rate panel makes drop visible |
| **Streaming/TTFB practice** | First-byte latency, stall detection, abandonment | Gaps closed: `stream.first_event`, `stream.gap`, `stream.abandoned.count` |
| **Long-running-workflow reliability** | Watchdog for runs that never finish; resume observability | Gaps closed: `run.stuck.count`, `workflow.checkpoint_resume.count` |
| **Security observability** | Instrument the guarantee, not only the failure | `guardrail.token_leak.count` + `telemetry.redaction.count` |
| **Data-quality / retrieval effectiveness** | Measure the corpus against the query, continuously | `products.retrieved`, `zero_result{reason}`, and the derived search-hit ratio (D7) |

### 4.2 Business standards

| Standard | What it mandates | Cross-check result |
|---|---|---|
| **Task success (HEART)** | Did the user accomplish the task — not just "no error"? | Gap closed: `request.component_filled_ratio`, `component.shortfall.count` — a run can succeed with a component unfilled |
| **Funnel / drop-off analytics** | Conversion at each step | `funnel.stage.count` + `run.failure_reason` |
| **Refusal / out-of-scope rate** | A legitimate refusal is still a signal | Visible in the funnel; promoted as a derived trend |
| **Recommendation quality** | Output shape, diversity, relevance | `rerank.relevance`, setup shape, `retry.outcome`; quality *scores* from §4.4 (E8) |
| **RAG groundedness / hallucination guard** | Every recommendation traceable to retrieved evidence | `validator.rejection{reason=sku_not_retrieved}` (D6) |
| **GenAI evaluation (Foundry evaluators)** | Groundedness, relevance, coherence, tool-call accuracy, task completion | **Included** (E8/E8b): `workspace.eval.*` via continuous evaluation on sampled traces |
| **Unit economics** | Cost per successful unit, wasted spend | `cost.estimated`; cost per approved setup derived |
| **Engagement / abandonment** | Where users leave | Gap closed: `stream.abandoned.count{stage}` |

### 4.3 Evidence tiers

Every metric in §3 carries a tier. It exists so the later review knows what to trust before the baseline
period, and so nothing is presented as fact that is not yet measured.

- **Tier 1 — established external practice.** The methodology and the metric shape come from a standard
  (SRE/Google, RED, USE, OTel GenAI conventions, FinOps, product analytics). The metric's *existence* is
  not in question.
- **Tier 2 — grounded in this repository.** The README, `AgentFoundryRegistration`, `azure.yaml` or a
  shipped test records the failure mode the metric watches.
- **Tier 3 — hypothesis.** Standard-shaped but unverified for this agent: thresholds, latency budgets and
  quality proxies. **Do not alert on a Tier 3 metric until the baseline period has produced a range.**

### 4.4 Checked and rejected

| Considered | Why it is not in the inventory |
|---|---|
| Cache hit rate | The pipeline has no cache |
| AARRR acquisition/referral/retention | Not meaningful for one internal calling application |
| Per-caller / per-tenant metric tags | Unbounded cardinality; belongs in log queries |
| DORA deployment metrics | Belong to CI/CD, not the runtime agent |
| Unique users / runs-per-customer as a metric | Same cardinality problem |
| `workspace.budget.utilization` | Redundant with `budget.headroom` |
| `workspace.rerank.duration` | Redundant with `node.duration{node=rerank}` (DC7) |
| `workspace.setup.provenance_violation.count` | Redundant with `validator.rejection{reason=sku_not_retrieved}` (D6) |
| `workspace.catalogue.search_term_hit_ratio` | Not measurable from the tool contract (D7); the term-level claim stays a test |
| Explicit queue wait-time | No queue we control (Little's Law row) |

---

## 5. Dashboards

**Two separate Workbooks** (decision E1), cross-linked by a tile that opens the other board in the same
time window. Parameter names are identical in both so the link translates cleanly. Metrics land in
`customMetrics`, spans in `requests`/`dependencies`, logs in `traces`, failures in `exceptions`.

**Parameter bar (both boards):** time range, `deployment.environment`, `service.version`,
`run.prompt.version`, `model`, `run.status`, `component`, `slot`, `tool.name`, `stage`, `attempt.number`.

The boards have **distinct roles** (decision E2): the technical board answers *where did it happen*; the
business board answers *what is going wrong*. Every duration panel shows p50/p95/p99 (E3) and carries
exemplars (E4).

### 5.1 Technical board

1. **SLO banner** — availability `(success+rejected)/total`; p95/p99 run duration; in-flight runs;
   guardrail token leaks (must be 0); redaction count; sampling rate.
2. **Traffic and health** — runs by `run.status`; node duration p50/p95/p99 by node; node success/fail
   count (DC6); node time split deterministic vs model; run duration p50/p95/p99 with budget line.
3. **Model** — input/output tokens **by stage** (DC1); time-to-first-token p95; time-per-output-token
   p95; transport retries; timeout count; bounded errors by `error.type`; throttled (429) rate; contract
   violations by stage; tokens/minute against quota; cost per run.
4. **Catalogue / MCP / tools** — retrieval yield per component; zero-result per component with
   catalogue/budget reason; search-hit ratio; tool calls by `tool.name`; **per-tool availability tiles
   (DC4)**; MCP connection p95 and failures; catalogue timeouts; auth failures missing vs expired.
5. **Failures and diagnostics** — terminal failure-reason taxonomy; retry-reason taxonomy; validator
   rejections by reason; **review issues: count and code breakdown (DC2)**; exceptions by type; recent
   traces (run.id, status, duration, slowest node, open); exporter failures; stuck runs.

### 5.2 Business board

1. **Headline KPIs** — runs today (WoW); success rate; unavailable rate; time-to-first-candidate p50/p95
   (DC3); approved setups; cost per successful run; cost per unavailable run; guardrail leaks.
2. **Funnel** — started → verified → retrieved → composed → approved; stage drop-off waterfall; retry
   outcome by attempt.
3. **Customer intent and demand** — component demand mix; slot demand; budget stated vs derived; budget
   headroom distribution; products excluded by ceiling per component.
4. **Recommendation quality** — component filled ratio; component shortfall per component; approved setups
   per run; setup monthly total price band; setup lines / distinct SKU / coverage; reranker relevance mix;
   retrieval coverage headline; Foundry evaluator scores.
5. **Outcome economics** — cost per run by outcome; productive vs wasted spend; business failure taxonomy
   over time.

### 5.3 Board rules

- The panels show counts, amounts and rates. The business board **may drill into content** (decision E6)
  by opening the underlying framework span — gated by the **same RBAC as the technical board**, never a
  wider audience. Because content drill-down exists, decision B2 (token redaction) must be proven before
  it ships.
- Metrics are the **unsampled safety floor**; spans and logs are the sampled detail (E5). A "0" on a
  safety counter must never mean "sampled away".

---

## 6. Alerts and SLOs

Kept off the dashboards. Boards show; alerts page. Thresholds wait for the baseline (F4), **except** the
zero-expected alerts, which ship immediately because a single occurrence is meaningful regardless of
baseline.

| Alert | Condition | Severity | Threshold? |
|---|---|---|---|
| Token leak | `workspace.guardrail.token_leak.count > 0` | Critical | no — zero-expected |
| Exporter failure | `workspace.telemetry.exporter_failure.count > 0` | Critical | no — zero-expected |
| Redaction fired | `workspace.telemetry.redaction.count > 0` | Warning | no — investigate |
| Availability | `unavailable / total` above threshold over 15 min | Critical | baseline |
| Latency | p95 `workspace.run.duration` above budget over 15 min | Warning | baseline |
| Stuck runs | `workspace.run.stuck.count > 0` | Warning | zero-expected |
| Retrieval regression | `zero_result` per component above baseline | Warning | baseline |
| Model throttling | `workspace.model.throttled.count` rate above threshold | Warning | baseline |
| Prompt regression | `contract_violation` per stage above baseline | Warning | baseline |
| Evaluator quality | `workspace.eval.*` below baseline | Warning | baseline |
| Cost | daily estimated cost anomaly | Warning | baseline |

SLOs (Google SRE shape): availability = `(success + rejected) / total` — `rejected` is a legitimate
outcome, so only `unavailable` counts against availability; latency = p95 run duration; both with an
error-budget burn view. Statistical thresholds are **Tier 3** until the baseline period sets them.

---

## 7. Configuration

Environment only; no secrets in the repository.

| Variable | Purpose |
|---|---|
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Selects the Azure Monitor exporter. Injected by the platform in a hosted container; uncommented in the gitignored `.env` locally |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Local-development alternative (Aspire Dashboard); `:4317` gRPC |
| `OTEL_SERVICE_NAME` | Must equal the deployed agent name |
| `OTEL_RESOURCE_ATTRIBUTES` | `deployment.environment`, `service.version` |
| `OTEL_TRACES_SAMPLER` / `_ARG` | Head sampling under the 5 GB allowance |
| `OTEL_INSTRUMENTATION_GENAI_CAPTURE_MESSAGE_CONTENT` | Decision A2: content capture, read from configuration, **default documented as a temporary on** (F6) |

Never define a setting of our own with a `FOUNDRY_` prefix — the platform reserves it.

---

## 8. Delivery plan

Each slice is one revertible commit and ends with the baselines green.

| # | Slice | Content | Verification |
|---|---|---|---|
| S1 | Spike (throwaway) | Confirm the distro is active with no config and what it exports; the exact OTLP/Azure env surface; that MAF `UseOpenTelemetry` on a **stage** agent emits spans; whether checkpoint/resume breaks run-span nesting; that metrics are unaffected by trace sampling; whether the exporter emits exemplars | `specs/archive/spikes/SPIKE-observability.md` |
| S2 | Telemetry vocabulary | `WorkspaceTelemetry` (one `ActivitySource`, one `Meter`, instruments), tag-name constants; one type per file | Unit test: instrument and span names match the vocabulary; no duplicates |
| S3 | Run boundary | Run span (child of `Activity.Current`) in the input node; `run.count/duration/attempts/failure_reason` in the three completion executors | Test: span exists, status set, error status on failure |
| S4 | Node boundary | Node span + `node.duration/count` in `WorkspaceSuggestionStreamingExecutor.HandleAsync` and the input node; guardrail abort recorded as a failed node, content-free | Test: one span per executed node, parented to the run span |
| S5 | Retrieval / RAG | `catalogue.search.*`, `products.retrieved`, `zero_result{reason}`, `excluded_by_ceiling`, `connection.*`, `auth_failure`, `tool_available` | Test: tags are bounded enums only |
| S6 | Composition / review / budget | `candidate`, `setup.*`, `validator.rejection{reason}`, `review.issues.count`, `review.issue.count{issue.code}`, `budget.*`, `request.component_filled_ratio`, `component.shortfall`, `retry.reason` | Test: retry, rejection, unfilled-component and issue-code paths emit |
| S7 | Framework adoption (A5, C5) | `UseOpenTelemetry` on stage agents and the shared chat client **only**; remove the bespoke `stage-model-call` span; keep `AgentRunUsage`; enable content capture | Test: served agent still unwrapped; stage name present; no duplicate chat span |
| S8 | Redaction, host, docs | Token-redaction span processor on guardrail fire; subscribe our source and meter; sampling; document the environment surface; SLO/alerts/exemplars | Test: a token-leaked answer produces no token in any span or log |
| S9 | Local development | Aspire Dashboard instructions over OTLP; no Collector | Manual: one local run visible end to end |
| S10 | Dashboard + contract + cardinality + baselines | Two Workbooks; the DC1–DC6 corrections; telemetry-vocabulary test; cardinality guard; PII/token guard; record the AGENT baseline | `dotnet test agentfoundry/AgentFoundry.sln`, Release build, `specs/baselines.md` updated |
| S11 | Foundry continuous evaluation (E8/E8b) | Connect continuous evaluation to sampled App Insights traces; emit `workspace.eval.*`; evaluator sample rate and cost cap | Quality metrics visible on the business board |

**Definition of done:** Release build green; `dotnet test agentfoundry/AgentFoundry.sln` green (baseline
**146 passed** today); baselines recorded; no provider SDK added to the application; no secret in the
repository; the caller's token appears in no span and no log.

---

## 9. Evidence, validation and the delete rule

1. **S1 spike** confirms every platform assumption in §1 before any exporter assumption is trusted.
2. **Baseline period** (target one to two weeks) collects with **no statistical alerts**; thresholds are
   reference lines only (F4). This turns Tier 3 into Tier 2.
3. **Content-capture review** at the end of the baseline (F6): decide A2 on measured byte volume under the
   5 GB allowance and the redaction counter. Keeping it on is a decision that must be evidenced, not
   assumed.
4. **Delete rule** (F5): a metric that never changes a decision is removed at the review. Instrumentation
   costs ingestion and cardinality; it must earn its place. `rerank.duration` and
   `setup.provenance_violation.count` were removed by this rule before being built.
5. **Re-grade** metrics only from observed data, and record it here.

---

## 10. Open items

- The Foundry evaluator sample rate and cost cap are set in S11; the evaluators' own model spend is
  separate from the agent's.
- Stale documentation found while researching, to fix in a separate commit: `agentfoundry/README.md` §2.4
  names the guardrail `ModelOutputGuardrailChatClient`, but the class is `GuardrailChatClient`; §1.1 says
  "five stage agents" while `WorkspaceSuggestionAgentRoster.All` holds six.
- The validator's violation prefixes are a string format rather than a typed classification; S6 may add a
  typed reason so telemetry does not parse strings.

---

## 12. Implementation status

Built in one pass. The AGENT baseline moved **146 → 150** (`specs/baselines.md`).

| Task | Status | Where |
|---|---|---|
| T0.1 Spec sync | **done** | this file |
| T0.2 Stale README facts | **done** | `agentfoundry/README.md` |
| T1.1–T1.7 Spike | **not done** — needs the agent run against an OTLP sink; every platform assumption in §1 is still observed-not-run | `specs/archive/spikes/` (absent) |
| T2 Vocabulary | **done** | `Shared/Telemetry/WorkspaceTelemetry.cs` |
| T3 Run boundary | **done** | `TelemetryChatClient`, the entry node, the three completion nodes |
| T4 Node boundary | **done** | `WorkspaceSuggestionStreamingExecutor` |
| T5 Retrieval + validator + tool calls | **done, less one** | `WorkspaceWorkflowTelemetry`, the retrieval and validator executors, `RecordingMcpToolFunction` |
| T5.2 `contract_violation` | **deferred** — no shared parse seam exists yet; recording it needs one that the repo does not have | — |
| T6 Composition/review/budget | **done** | `WorkspaceWorkflowTelemetry` |
| T7 Framework adoption | **done** | `AgentFactory`, `WorkspaceSuggestionAgentBuilder` |
| T8.1–T8.4, T8.6 Redaction, host, MCP | **done** | `GuardrailChatClient`, `TokenLeakRedactionProcessor`, `Program.cs`, `RecordingMcpToolFunction` |
| T8.5 Env docs | **done** | `.env.example`, `appsettings.json` |
| T9.1 Local dev docs | **partly** — the env surface is documented; the Aspire Dashboard run-through is not written | `.env.example` |
| T10.4 Vocabulary test | **done** | `ObservabilityTelemetryTests` |
| T10.5 Cardinality guard | **partly** — the vocabulary test pins names; a tag-key guard is not a separate test | `ObservabilityTelemetryTests` |
| T10.6 PII/content guard | **partly** — the guardrail mark is proven; "our spans carry no content" is not yet a test | `ObservabilityTelemetryTests` |
| T10.7 Baseline | **done** | `specs/baselines.md` (150) |
| T10.1–T10.3 Workbooks | **not done** — Azure Monitor workbooks need portal/subscription access | — |
| T11 Foundry continuous evaluation | **not done** — needs a Foundry evaluation pass and its own access | — |

Known deltas from the plan text, recorded so the next reader is not surprised:

- `run.duration` is the span's duration, not an instrument (§3.2).
- `model.contract_violation` and a standalone cardinality/PII test remain open (T5.2, T10.5, T10.6).
- Retrieval metrics are recorded per attempt in the retrieval executor, because the state keeps only the attempt
  in flight and a retry would otherwise hide the first attempt's empty searches.
- The run span is closed by each of the three completion nodes (`CompleteRun`), and the run's outcome metrics are
  read from the state by the node bracket (`WorkspaceWorkflowTelemetry.RecordRunOutcome`).

---

## Appendix — representative queries

```kusto
// Availability: rejected is legitimate, unavailable is the signal
customMetrics
| where name == "workspace.run.count"
| summarize total = sum(value) by status = tostring(customDimensions["run.status"])

// p95 node duration, by node and kind
customMetrics
| where name == "workspace.node.duration"
| summarize p95 = percentile(valueSum / valueCount, 95)
    by node = tostring(customDimensions["node"]), kind = tostring(customDimensions["node.kind"])

// Retrieval yield per component, split by tool
customMetrics
| where name == "workspace.catalogue.products.retrieved"
| summarize yield = avg(valueSum / valueCount)
    by component = tostring(customDimensions["component"]),
       tool = tostring(customDimensions["tool.name"])

// Terminal failure taxonomy (retry reasons live on workspace.retry.count)
customMetrics
| where name == "workspace.run.failure_reason.count"
| summarize failures = sum(value) by reason = tostring(customDimensions["reason"])

// Groundedness: the sku_not_retrieved validator reason should trend to zero
customMetrics
| where name == "workspace.validator.rejection.count"
| where customDimensions["reason"] == "sku_not_retrieved"
| summarize violations = sum(value) by bin(timestamp, 1h)
```

Histograms from the OpenTelemetry exporter arrive as `valueSum`/`valueCount`/`valueMin`/`valueMax`;
confirm the exact shape in S1 and pin it in the workbooks.
