# e05s01 — One request in, one typed result out, streamed

**type:** feat
**risk:** P0
**context:** agent
**bcps:** 5
**status:** in-progress

## Context

This is the tracer bullet: a deployable that speaks the Responses protocol, runs one workflow, and
answers with the **typed result** everything downstream depends on. It also writes the **contract
schemas** both trees compile against, which is why it comes before the agents.

Only one cheap agent is registered here, so the protocol, the hosting package and the schema are all
exercised before any model call is spent on composition. The rephraser and suggestor attach in
`e05s02` and `e05s03`.

## Requirements

#### ADDED: the contract

Under `agentfoundry/shared/contracts/`:

- **request** — `runId`, `query`, `currency`, `slots[]` (`slot`, `capacity`), `ceilingMonthly` (nullable)
  and `catalogue[]` — the **whole compact projection, `Description` included**, as it exists today.
- **result** — `status` (`suggested` | `notWorkspace`), `reason`, `options[]` and `usage`. An option
  carries `lines[]` (`slot`, `sku`, `quantity`, `why`) and `rationale`. **No price, no product name and
  no image crosses**, and no band label is carried: the application sorts and labels.
- **workspace-spec** — the intermediate the rephraser produces and the suggestor consumes.

The application keeps its own DTOs and one test pinning them to these schemas.

The slot-name enum is **inlined in each file** rather than referenced across them, so every schema is
self-contained: any consumer — the application's contract test (`e05s05`), an agent test, a validator —
can check an instance with no registry to set up. The duplication is deliberate and guarded: a test
asserts the three files carry the same enum, so drift fails the build rather than a run.

Verified with `jsonschema` 4.26 (Python) while the agent tests were being written: a valid result passes;
an unknown slot is rejected; an unexpected `price` property is rejected by `additionalProperties: false`;
`quantity: 0` fails the minimum. Two of the design's rules are therefore enforced by the contract itself,
not only by the application.

#### ADDED: the hosted agent and its tracer

One project in `agentfoundry/`, hosted as a Foundry hosted agent — code deploy, `runtime: dotnet_10`,
`protocols: [responses]` — with `AgentHost.CreateBuilder(args)`, `AddFoundryResponses`, and the protocol
registered by `builder.RegisterProtocol("responses", endpoints => endpoints.MapFoundryResponses())`, and
with a single agent registered under the name the host resolves. Built on the framework's own factories
(`AIProjectClient.AsAIAgent`, `ChatClientAgentOptions`, `ChatResponseFormat.ForJsonSchema<T>`).

> Verified by scaffolding (`azd ai agent init`) and building: `AgentHost` comes from
> `Azure.AI.AgentServer.Core` in `Microsoft.Agents.AI.Foundry.Hosting`. The `WebApplication.CreateBuilder()`
> + `app.MapFoundryResponses()` shape does **not** apply to the current package.

#### RECORDED: the constraint this story must not break

The run carries ~172k input tokens, so it fits **only on a standard `gpt-4.1-mini` deployment**
(300k context); provisioned-managed and batch deployments are 128k and cannot run it. The deployment
type belongs in configuration and in the deployment template, not in a comment.

#### ADDED: a self-contained project

Deploy is **direct code**: Foundry zips the project folder and builds it, so the repository root's
`Directory.Packages.props` is not in the zip. The project therefore declares **exact package versions**
and overrides **two** properties — `ImportDirectoryPackagesProps=false` **and**
`ManagePackageVersionsCentrally=false` — because the root switches central management on in
`Directory.Build.props` as well as `Directory.Packages.props`. Setting only the first leaves the second
in force and every `Version=` attribute becomes NU1008; this was verified by building. It must build
with nothing outside its own folder — no copied policy files, no inherited pinning.

The pins are Microsoft's own set from that sample: `Microsoft.Agents.AI.Foundry` and
`Microsoft.Agents.AI.Foundry.Hosting` **1.20.0-preview.260831.1**, `Microsoft.Agents.AI.Workflows`
**1.20.0** (the same 2026-08-31 release), `Azure.AI.Projects` **3.0.0-beta.2**, `Azure.Identity`
**1.21.0**. The hosting package is preview-only, so there is no fully stable option; the dates agreeing
is what makes the set coherent rather than merely current.

#### ADDED: the structured-output proof

This story was to settle whether `ChatResponseFormat.ForJsonSchema<T>()` works on the **Responses** path for
`gpt-4.1-mini`, which the whole design assumed and which nothing had verified. **Most of it is settled, and
at `e05s07` rather than by a deployment.**

Settled by measurement, offline, through the production client against a stand-in on a real socket: the schema
travels as the protocol's own field — `text.format = { type: "json_schema", name: <contract>, schema }` — with
**`tools` empty**. So this is native structured output, not the synthetic-tool route a fallback would have
taken, and the client half never needed a deployment. It is guarded by `StandInTransportTests`.

**Two things this story stated were wrong, and are corrected rather than quietly dropped.**

- **`UseStructuredOutput` does not exist.** Searched every method, public and non-public, of
  `Microsoft.Agents.AI`, `.Abstractions` and `.OpenAI` at the pinned versions, and the whole package cache: no
  member, no string. The fallback was never adoptable.
- **The call-count arithmetic had no derivation.** Both agents declare a schema, so a per-agent fallback costs
  **four** calls, not three. The "three" is withdrawn, and the two-call basis for the p95 ≤ 30 s target stands
  untouched because it rests on the workflow's shape rather than on the fallback.

What remains, and is genuinely deploy-only: the format carries **no `strict`**, so the API does not enforce
conformance. Whether `gpt-4.1-mini` returns a conforming object is therefore open — and bounded, because a
non-conforming answer is refused when the result is read (AIWB-03) and validated as a whole before anything is
rendered (`e05s08`). If it fails, the repair is **ours to build**: validation-driven retry, or plain-text
prompting with the reader's existing tolerance. Not a framework switch.

#### RECORDED: what the framework does with output that breaks the contract

Writing this story's tests established two things the design has to live with. Both were **found**, not
assumed, and both now have a test behind them:

- **The typed result is deserialized lazily.** `RunAsync<T>` returns without complaint for output that is
  not the contract; the refusal surfaces when the result is **read**. Reading it is therefore part of the
  contract, not an optimisation — an agent that never reads it would pass a malformed answer downstream
  unnoticed.
- **The deserializer is the only thing enforcing the contract on this path.** Prose, and a slot outside
  the vocabulary, are refused; a `quantity` of **zero** and an unexpected **`price` property are accepted
  silently**. The schema is the contract, but nothing here enforces it — which is why the application's
  validation (`e05s08`) has to carry `quantity ≥ 1`, and why a stray property needs no handling at all:
  the application never reads a price from the agent, so one cannot leak.

The gap is pinned by a test on purpose: if the framework ever begins enforcing the schema, the test fails
and the change is noticed rather than the discovery being lost.

#### RECORDED: how the host itself behaves

The host test (AIWB-04) starts the real `AgentHost` over a fake-backed agent and speaks to it, which
established three things worth not rediscovering:

- **`AgentHostApp` exposes `RunAsync(CancellationToken)`, not `StartAsync`/`StopAsync`.** It also exposes
  the underlying `WebApplication` and a blocking `Run()`. `RunAsync` with a token is what lets a test
  start the host, speak to it and stop it.
- **The host binds its own port: 8088 by default**, honouring the **`PORT`** environment variable and
  **ignoring `ASPNETCORE_URLS`**. A test should take a free port through `PORT` rather than squat 8088,
  and a local stand-in must be told the same way.
- **The hosted Responses path streams through the streaming member of the chat client.** A fake that
  implements only `GetResponseAsync` makes every host call fail with `server_error` — which is exactly how
  the first version of this test failed, and worth knowing before the browser tier's stand-in is written.

## Zoom-Out

- **Unit purpose:** a new deployable beside the application. It owns reasoning and formatting; it owns
  no catalogue data, no prices and no entitlement.
- **Callers:** the application, over the agent endpoint (`e05s05`).
- **Contracts to preserve:** the schema is the **only** artefact shared with the application, and the
  application remains the only place a price or a product name is resolved.

## Steps

1. Add the **`WorkspaceSuggestions`** project to `agentfoundry/AgentFoundry.sln` with **exact** MAF
   package versions and **both** central-management overrides (`ImportDirectoryPackagesProps=false` and
   `ManagePackageVersionsCentrally=false`), so it builds with nothing outside its own folder.
   → verify: `dotnet build agentfoundry/AgentFoundry.sln -v q --nologo`
2. Write the three contract schemas; the request schema carries the projection **including**
   `Description`. → verify: `test -f agentfoundry/shared/contracts/suggestion.request.schema.json && test -f agentfoundry/shared/contracts/suggestion.result.schema.json && test -f agentfoundry/shared/contracts/workspace-spec.schema.json`
3. Register one tracer agent with structured output and host it with the Responses protocol.
   → verify: `dotnet build agentfoundry/AgentFoundry.sln -v q --nologo`
4. Tests AIWB-01 to AIWB-04 against a fake model client, no network.
   → verify: `dotnet test agentfoundry/AgentFoundry.sln --nologo`
5. Deploy the tracer and prove that `gpt-4.1-mini` returns a typed result on the Responses path, recording
   the model call count. **The client half is already proven offline** (the schema travels as `text.format`
   json_schema, natively, no synthetic tool — `StandInTransportTests`). What only a deployment can settle is
   whether the model **honours** it; conformance is not enforced (`strict` is absent), so a non-conforming
   answer is possible and is absorbed by validation and the reader. The fallback is the application's to
   build, not a framework switch.
   → verify: `dotnet test agentfoundry/AgentFoundry.sln --nologo --filter "FullyQualifiedName~ResponsesStructuredOutputProbe"`
