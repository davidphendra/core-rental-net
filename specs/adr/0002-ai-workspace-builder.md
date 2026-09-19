# 0002 — The AI workspace builder: two deployables, one contract

**Status:** accepted
**Date:** 2026-09-18
**Deciders:** product owner, engineer

## Context

A customer types what they have in mind for a workspace, and the application answers with setups
drawn from the catalogue, each applicable with one confirmed click. The reasoning runs in a Microsoft
Foundry hosted agent built on Microsoft Agent Framework (MAF).

Constraints that shaped this:

- The application ships three green baselines, and its NONBROWSER suite must stay **100% offline**.
- The catalogue is 205 products across seven slots with fixed capacities (`Desk 1, Chair 1, Monitor 3,
  Lamp 1, Plant 1, CoffeeStation 1, RelaxZone 1`). Its price spread is wide — a full seven-slot setup
  runs from 497k to 16.9M IDR/month — and the application is the only authority on names and prices.
- A run is paid, customer-visible and abusable in a way the rest of the application is not.

## Decision

**Two deployables, one shared artefact.** The agent lives in `agentfoundry/`, in its own solution, with
no project reference in either direction. The only thing the trees share is the contract schema under
`agentfoundry/shared/contracts/`. The application keeps its own DTOs and a test pinning them to those
schemas.

**One hosted agent containing two agents.** A single Foundry hosted agent (code deploy, `runtime:
dotnet_10`, `protocols: [responses]`), hosted with `AgentHost.CreateBuilder(args)` — which binds the port,
serves the readiness probe and wires OpenTelemetry — with `AddFoundryResponses` and the protocol
registered through `builder.RegisterProtocol("responses", endpoints => endpoints.MapFoundryResponses())`.
Inside it, **two** MAF agents — `rephraser`, then `suggestor` — composed into **one sequential workflow**
and published as a single `AIAgent`. Default chaining, so the suggestor keeps seeing the request.

**The agent deploys as code, and its project is self-contained.** Direct code deploy means Foundry zips
the **project folder** and builds it — the root's `Directory.Packages.props` is not in that zip — so the
project declares **exact package versions** and overrides **two** properties: `ImportDirectoryPackagesProps=false`
and `ManagePackageVersionsCentrally=false`. The second is not optional — the root switches central
management on in `Directory.Build.props` *as well as* `Directory.Packages.props`, so disabling only the
latter still makes every `Version=` attribute an NU1008 error. Verified by building: without the second
override the agent project cannot compile inside this repository. Its build
then depends on nothing outside its own folder, and the repository's one-version-source rule does not
reach it. Container deploy would have preserved central pinning by using the repository root as build
context, at the cost of Docker and a container registry the deploy guidance tells us not to acquire for
this. The pins are the set Microsoft ships together in its .NET hosted sample —
`Microsoft.Agents.AI.Foundry` and `Microsoft.Agents.AI.Foundry.Hosting` **1.20.0-preview.260831.1**,
`Microsoft.Agents.AI.Workflows` **1.20.0** (the same 2026-08-31 release), `Azure.AI.Projects`
**3.0.0-beta.2** and `Azure.Identity` **1.21.0**. There is no fully stable option: the hosting package is
preview-only.

**The whole projection is pushed, on both calls.** The agent receives the compact catalogue as it
exists, **`Description` included** — measured at **~336 KB ≈ 86k tokens**, present on the rephraser's
call and again on the suggestor's. The agent is tool-less and never calls back into the application.
The saving from trimming the payload was considered and rejected: the metadata that makes qualitative
matching possible is retained, and the two hops were preferred over a custom input-shaping executor.

**The model is `gpt-4.1-mini` on a standard deployment.** Its context is 1,047,576, but **300,000 on
standard deployments against 128,000 on provisioned-managed and batch deployments** — so the ~172k
tokens a run consumes fit only on a **standard** deployment. This is a deployment constraint, not a
preference. Whether the model returns **structured output on the Responses path** is **half proven and half
left to a deployment**, corrected at `e05s07` by measurement rather than assumption. What is settled offline:
the schema travels as the protocol's own `text.format = { type: "json_schema", name, schema }`, with **no
synthetic tool** — so the client half works and always did. What is not settled: the format carries **no
`strict`**, so conformance is not enforced by the API, and whether `gpt-4.1-mini` returns a conforming object
is a question only a deployment answers. It is bounded either way — a non-conforming answer is refused when
the result is read and validated as a whole before anything is rendered (`e05s08`). **The fallback named here
previously, `UseStructuredOutput`, does not exist in the pinned framework** (no member and no string in any of
the three MAF assemblies or the package cache), and the three-call figure it implied was never derived: both
agents declare a schema, so a per-agent fallback would be four. If the model does not honour the format, the
repair is the application's to build.

**Latency and cost.** Target **p95 ≤ 30 s**, with a **45 s hard timeout** — deliberately *above* the
target, so a slow-but-correct run finishes rather than being cut off at exactly the number it was allowed
to reach. A target and a timeout at the same number would make the target partly an outage rate, and a run
cut at the limit has already spent ~172k tokens and returns nothing. There is **no monetary ceiling**;
tokens, model and model call count are recorded per run so the evaluation tier sets the per-customer cap
from measurement, and so the cap is defensible rather than guessed.

**The model's own text streams; the result stays whole.** The customer sees the model's words during the
run, revealed **as each JSON string value completes** — never raw JSON, and never token-by-token, which
is also what lets hygiene run on a **complete** value before display. Both the reading and the hygiene
run **in the application**, where the display is; the agent therefore holds no customer-facing code.
Streamed text is **purpose only**: no prices, no product names, with currency amounts stripped as a
deterministic backstop. The structure is validated as a whole before any candidate is rendered.

**Failure and cancellation are honest.** Streamed text is **kept and marked not applied** on both paths.
Cancelling is a neutral *stopped* state, logged as cancelled, and applies nothing. Stages are retained
and collapsed after a run. There is **no fallback**: a run that fails says so and offers a retry.

**The application validates, and owns everything that matters.** All-or-nothing: every SKU exists, its
slot is allowed, and `quantity ≤ capacity`. Prices, names and totals are resolved by the application,
never taken from the agent.

**Three candidates, labelled by rank, and fewer is fewer.** The agent proposes three genuinely
different setups; the **application** sorts them by monthly total and labels them Budget / Balanced /
Premium. Labels are relative, because with a 30× price spread and partial compositions an absolute band
would classify a narrow request into one band three times. The specification carries the customer's
**stated monthly ceiling when they give one**; it never carries a band label. If the catalogue supports
only two distinct answers, two are shown — labels by rank, and the panel tolerates one to three.

**Applying replaces the composition, in one confirmed atomic write.** Every slot is written from the
candidate; the **delivery address is untouched**. A stale write is refused loudly and applies nothing.

**Entitlement is decided by the application, and closed when unconfigured.** A signed-in customer holding
a permission declared in configuration (`Authorization:<Section>:ClaimType` / `ClaimValue`); with the
section unconfigured the AI section is **absent**, not open. One run in flight per customer. **No numeric
per-customer cap until measurement exists.**

**Identity carries no secret.** `DefaultAzureCredential` on both hops — the application to the agent,
the agent to the model — so it resolves to a developer credential locally and a managed identity when
deployed. No endpoint key and no model key appear in configuration.

**The application calls the hosted agent as a MAF agent.** The adapter derives the per-agent OpenAI
endpoint from the project endpoint and the agent name
(`<project>/agents/<name>/endpoint/protocols/openai`), builds a `FoundryAgent` with
`AIProjectClient.AsAIAgent(agentEndpoint)`, and streams with `RunStreamingAsync`. Configuration
carries a project endpoint and a name, never a URL.

**A run uses no session, or a fresh one.** Never a reused session: the hosted agent is a singleton
shared across customers, so a session carried between runs would carry one customer's request into
another's.

**Prompts are versioned files embedded into the assembly.** `prompts/<role>.v1.md`, ordinary markdown in
the repository, embedded at build so a hosted agent cannot start without its instructions. The run
record names the version. **No skills** are attached; the agents' instructions plus the catalogue plus
the structured contract are the whole of it, and a skill is added only if the evaluation demands one.

**One run record, no PII, 90 days.** The query, a **hash of the projection actually sent**, model and
prompt versions, tokens, model call count, latency, the **model's raw structured output**, the
validation verdict, an opaque customer id, and which candidate was applied. The raw output is kept
because a rejected run is otherwise unexplainable, and under the purpose-only rule it carries no price
and no product name. Only the query and the catalogue cross to Foundry — no identity, no address, no
draft.

**The feature is off unless explicitly enabled**, and the browser tier runs against a **local stand-in
speaking the same protocol**, so the real adapter and the streaming path are exercised with no network.
The stand-in lives in the **application's test tree** as a test-only project pinned to the shared schema:
it is an artefact of the browser tier's hermeticity, not of the agent, and it must not put a test double
in a tree whose rule is no reference in either direction.

## Consequences

**Good**

- The application's baselines cannot be moved by agent work: the AI sits behind one port with a
  hand-written fake, and a Foundry or model outage cannot fail the suite.
- No SKU and no price can reach a customer unvalidated; the application recomputes every amount.
- Streaming gives the customer the model's words without ever showing raw JSON or an unhygiened value.
- Relative labels stay meaningful across the catalogue's whole price range and for partial compositions.
- One contract schema, versioned in one commit with the agent that consumes it.

**Bad**

- ~172k input tokens per run, on a model whose usable context depends on deployment type — a
  provisioned or batch deployment silently makes the run impossible.
- Two lifecycles in one repository; CI needs path-filtered jobs.
- The workflow's intermediate contract (`WorkspaceSpec`) is new surface to own and evaluate.
- A hosted agent is a singleton reused across customers: any stateful executor must reset between runs
  or leak across customers.
- The model's free text reaches the customer before validation; that is accepted only because the text
  is constrained to purpose and cannot carry a price or a name.

## Alternatives rejected

| Alternative | Why rejected |
|---|---|
| Agent projects inside `CoreRentalNet.sln` | A Foundry or model outage would fail the application's suite |
| A separate git repository | Duplicated build policy and a two-commit contract-drift window |
| One agent in one call | The rephraser buys a stable intermediate specification gradeable on its own |
| A verifier/reviewer pair as well | Pays model calls to re-check what the application checks deterministically |
| Trimming the payload, or a per-slot shortlist | Loses the metadata that makes qualitative matching possible, or adds a custom executor |
| The agent pulling the catalogue over the API | A second trust boundary and an incomplete-catalogue failure mode, for a payload small enough to push |
| A prompt agent defined in the portal | The agent's behaviour would live in a portal resource, not this repository |
| Absolute budget bands | Classify a narrow request into one band three times, making the labels meaningless |
| A silent fallback on failure | Makes a failure invisible and poisons the evaluation data |
| A numeric cap chosen before measurement | Looks tuned, is not, and nobody revisits it |
