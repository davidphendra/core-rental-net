# e05s09 — It can be measured, gated, and turned on deliberately

**type:** feat
**risk:** P1
**context:** app
**bcps:** 5
**status:** planned

## Context

The feature is a paid, quality-sensitive call, so it must be answerable after the fact: which prompt,
which model, how many calls, how much it cost, and what the customer asked. It must also be possible to
turn it on in one environment without turning it on everywhere, and to prove quality before customers
see it.

## Requirements

#### ADDED: the run record

One structured record per run: the query, a **hash of the projection actually sent** (not of the
catalogue file — a change to the projection is a change to what the model saw), model and prompt
versions, tokens, **model call count**, latency, the **model's raw structured output**, the validation
verdict, an **opaque customer id**, and which candidate was applied — with **bounded retention of 90
days**. No name, no email, no address.

The raw output is kept because the rejected run is the one worth debugging: it is the only evidence
that can tell a bad prompt from a bad catalogue, and under the purpose-only rule it carries no price and
no product name. It is also what the evaluation tier grades — the model's output, not what the
application salvaged from it.

The `modelCalls` field exists so the assumption of two calls per run can never drift unnoticed.

#### ADDED: the boundary assertion

A test that the request crossing to Foundry carries the query and the catalogue and nothing else — no
identity, no address, no draft state.

#### ADDED: the configuration gate, and the deployment constraint

`Agent:Enabled`, endpoint, model name, the **deployment type**, which must be **standard**: a run
carries ~172k tokens and a provisioned-managed or batch `gpt-4.1-mini` deployment is 128k, so it would
fail rather than degrade; and the **timeout, 45 s**. Off or unconfigured ⇒ the panel is absent and the
endpoint answers *unavailable*. Turning it on is a deploy-time decision, not a code change.

#### ADDED: the evaluation gate

`agentfoundry/tests/eval`: a golden set (vague, ceiling-constrained, contradictory, off-topic queries),
the deterministic checks that run offline, and a rubric tier that runs against the live model before the
feature is enabled, with the bar at **≥ 80% of golden-set queries meeting the rubric** alongside every
deterministic check passing. A prompt or model change re-runs it. **The tier's first job is to report
tokens and model calls per run**, because that measurement is what sets the per-customer cap (`e05s06`) —
no cap exists before it.

**Who owns it.** Engineering **drafts** the queries; the **product owner approves them** before the gate
is enforced, and the approval is recorded with the set. The queries are therefore not solely the feature
author's, which is the whole reason the gate can mean anything — and a later prompt change cannot quietly
relax the bar.

#### ADDED: the deployment and the local stand-in

The `azd` project, the Foundry project and the standard model deployment, the `infra/` templates, the
managed-identity RBAC that lets the application invoke the agent and the agent reach the model — and the
**local stand-in** that speaks the same protocol, so the browser tier exercises the real adapter and the
streaming path with no network.

The stand-in lives in the **application's test tree**, as a test-only project started by the browser
fixture and pinned to `shared/contracts/`. It exists for the browser tier; the agent repository stays a
deployable, not a test host.

> **It imitates a captured wire, not an imagined one.** `e05s04` ran the real host and recorded the shape:
> `response.created` / `response.in_progress`, then per executor a `workflow_action` item and a `message`
> item carrying `response.output_text.delta` whose `delta` is the **raw JSON**, then the terminal events —
> the specification's message item first, the result's message item last, each once, joined by a newline.
> The stand-in must reproduce that, including the two separate message items, or the browser tier proves
> nothing about the streaming path it is standing in for.

#### ADDED: continuous integration for the agent

`.github/workflows/ci.yml` currently restores, builds and tests **only `CoreRentalNet.sln`** — so as
things stand the agent's tests would never run on a push. A **path-filtered job** builds and tests
`agentfoundry/AgentFoundry.sln` in its own job, so an agent-only change runs only agent tests and an
application-only change does not wait on the agent. That isolation is the entire point of the two-tree
separation (ADR 0002), and without the job the separation is only nominal.

The job runs with **no Azure credential**: every agent test in it uses the fake model client, and the
live tier — the rubric, and the structured-output probe — stays out of it.

#### ADDED: the README correction

The repository README states that there is no deployment configuration and that nothing deploys. The
agent's `azure.yaml` and `infra/` make that false for the agent tree. The sentence is corrected to say
what is now true — **the agent deploys; the application does not** — rather than left to mislead the next
reader.

## Zoom-Out

- **Module purpose:** operability. It makes the feature measurable, gateable and reversible.
- **Callers:** operations, the evaluation tier, and the deployment pipeline.
- **Contracts to preserve:** no PII crosses to Foundry and none is stored; the feature is off unless
  explicitly enabled.

## Steps

1. Write the run record with no PII, the payload hash and the model call count; assert the boundary.
   → verify: `dotnet test tests/CoreRentalNet.Host.Tests --nologo --filter "FullyQualifiedName~AiRunRecordTests"`
2. Add the configuration gate, including the standard-deployment constraint.
   → verify: `dotnet test tests/CoreRentalNet.E2E --nologo --filter "FullyQualifiedName~AiBuilderGuardTests"`
3. Write the golden set and both tiers, and obtain the product owner's approval of the queries before the
   gate is enforced; record tokens and model calls for the set.
   → verify: `dotnet test agentfoundry/AgentFoundry.sln --nologo --filter "FullyQualifiedName~GoldenSetTests"`
4. Add the `azd` project, the standard model deployment, the `infra/` templates, identities and RBAC.
   → verify: `azd provision --preview`
5. Add the local stand-in and point the browser tier at it.
   → verify: `dotnet test tests/CoreRentalNet.E2E --nologo`
6. Run the rubric tier against the live model and record the result, with the measured cost.
   → verify: `dotnet test agentfoundry/AgentFoundry.sln --nologo --filter "FullyQualifiedName~RubricTierTests"`
7. Add a path-filtered CI job that builds and tests `agentfoundry/AgentFoundry.sln` with no Azure
   credential, leaving the application's job untouched.
   → verify: `dotnet build agentfoundry/AgentFoundry.sln --configuration Release -v q --nologo && dotnet test agentfoundry/AgentFoundry.sln --configuration Release --no-build --nologo`
8. Correct the README's "nothing deploys" statement so it says what is now true.
   → verify: `rg -n "the agent deploys" README.md`
