# e02s06 — The run is safe, and the run is measurable

**type:** feat
**risk:** P1
**context:** infra
**bcps:** 3
**status:** passing

## Context

Five decisions taken earlier in this initiative are only **falsifiable through measurement**, and four
specific risks are only safe through treatment:

| Needs measuring | Otherwise |
|---|---|
| the intent-table **miss rate** | the table rots and the model path silently becomes the normal path |
| the **exhausted rate** | no signal to justify strengthening the reviewer, or a second model |
| **catalogue calls and tokens per run** | the cost design stays an assertion |
| which class rejected — validator or reviewer | deterministic repairs and loop retries are indistinguishable |

| Needs treating | Why |
|---|---|
| catalogue text in the prompt | tool output is untrusted input: product names and metadata are data, never instructions |
| harmful or adversarial language | the user's free-text query can carry it |
| the Auth0 credential | it must never appear in a prompt, a trace or a log |

## Requirements

#### ADDED: one structured record per run

The agent emits **one structured record per run** carrying the outcome, the attempt count, the
catalogue call count, the token count, whether the slot set was table-derived or inferred, and how
often the validator repaired rather than the reviewer rejected. The agent also emits OpenTelemetry
traces (MAF has them built in), so a slow or looping run can be read rather than guessed at.

#### ADDED: a content-safety guardrail, and data-not-instructions

A Foundry content-safety policy is attached **to the deployed agent**, so one attachment covers all
four agents, and its declarative mirror is tracked under `agentfoundry/shared/guardrails/`. Catalogue
content is delimited and labelled as data in every prompt that carries it; a product field containing
instruction-like text changes no decision.

## Zoom-Out

- **Module purpose:** cross-cutting treatment and measurement. It changes no workflow behaviour.
- **Callers:** the operator reading a run, and the live tier reading AGT-18 and AGT-19.
- **Contracts to preserve:** the result schema is unchanged; the record is a log line and a trace, not
  part of the contract the application parses.

## Steps

1. Add the run record with its fields and emit it once per run. → verify: `dotnet build agentfoundry/AgentFoundry.sln -v q --nologo`
2. Add OpenTelemetry traces around the workflow nodes and the catalogue read. → verify: `dotnet build agentfoundry/AgentFoundry.sln -v q --nologo`
3. Assert the structural form of the data-not-instructions rule: catalogue text never crosses the
   contract, so there is no field an injection could travel in. The delimiter rule itself belongs with
   the adapter that writes a prompt, and there is no prompt yet. → verify: `dotnet test agentfoundry/AgentFoundry.sln --nologo --filter "FullyQualifiedName~ObservabilityTests"`
4. Track the guardrail declaratively under `agentfoundry/shared/guardrails/` and attach the policy to
   the agent (the attachment is verified live in `e02s07`). → verify: `test -f agentfoundry/shared/guardrails/workspace-suggestions.policy.yaml`
5. Unit tests AGT-18 and AGT-19: the record's fields, and an injected instruction-like product field
   that changes no decision. → verify: `dotnet test agentfoundry/tests/AgentFoundry.Tests --nologo`
6. Assert no credential can appear in a prompt or a trace, by construction. → verify: `dotnet test agentfoundry/tests/AgentFoundry.Tests --nologo --filter "FullyQualifiedName~SecretHygieneTests"`

## Verification Script (Step-by-Step)

1. Run one request and read the emitted record: outcome, attempts, catalogue calls, tokens, slot-set
   provenance.
2. Force a table miss and confirm the record says the slot set was inferred.
3. Force exhaustion and confirm the record shows three attempts and the outstanding findings count.
4. Put instruction-like text in a product name and confirm the run's decisions are unchanged.
5. Grep the run's trace and logs for the Auth0 client secret → nothing.

## Test matrix

| ID | Scenario | Level |
|----|----------|-------|
| AGT-18 | One structured record per run with its fields | unit |
| AGT-19 | Catalogue text cannot become an instruction | unit |

## Out of scope

- The golden-query evaluation dataset and continuous evaluation (deferred).
- Application-side logging, which belongs to `e04`.
- Any change to workflow behaviour.

## Risks

- **Instrumentation that is written but never read.** The record is deliberately small and its fields
  are the ones earlier decisions depend on; OPS-3's call and token counts are asserted, not observed.
- **A guardrail that is declared but not attached.** The declarative mirror can drift from the
  attached policy, which is why the attachment is asserted in the live tier.
- **Treating the trace as a debugging convenience.** It is the only way to see a loop that thrashed.

## Acceptance criteria

- AGT-18 and AGT-19 pass.
- The record carries the fields the earlier decisions need, and a table miss is visible in it.
- No credential appears in a prompt, a trace or a log.
- The application's three baselines are untouched.
