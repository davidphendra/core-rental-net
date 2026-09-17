# e02s02 — One request in, one typed result out, streamed

**type:** feat
**risk:** P0
**context:** infra
**bcps:** 5
**status:** passing

## Context

This is the tracer bullet for the agent: a container that speaks the Responses protocol, runs one
workflow, emits stage events as it goes, and answers with the **typed result** everything downstream
depends on — `ok`, `exhausted` or `rejected`, HTTP 200 for all three. It also writes the **contract
schema** that `e04` compiles against, which is why it comes before the other agent stories.

Only the verifier is implemented here. It is the cheapest gate in the workflow — one model call, no
catalogue — so the tracer bullet costs almost nothing to exercise.

## Requirements

#### ADDED: the suggestion contract

Three schemas under `agentfoundry/shared/contracts/`:

- **request** — `requestId`, `query`, and the **slot rule table** (`slot`, `displayName`,
  `maxQuantity`, `isMandatory`).
- **event** — `kind: "stage"`, `requestId`, `stage`, `attempt`.
- **result** — `kind: "result"`, `requestId`, `status`, `attempts`, `options[]`, and `code` or
  `findings[]` where they apply.

An option carries `tier`, `lines[]` (`slot`, `sku`, `quantity`), `criteria[]` (closed-vocabulary
tokens), `unevaluated[]` (`phrase`, `reason`) and `pinnedSlots[]`. **No prices, names or images cross
the boundary** — the application resolves them and recomputes every amount — and **no model prose is
carried**: criteria are tokens, and the only free text is the user's own phrase in `unevaluated[]`.

#### ADDED: the workflow and the verifier

One Microsoft Agent Framework workflow in one container: `Verifier → Rephraser → Suggestor →
Reviewer`, with findings looping back to the Rephraser. The verifier classifies the request and, when
it is not a workspace request, ends the run with `rejected`, a reason code and no options. The result
is streamed as stage events before it, and exactly once.

## Zoom-Out

- **Module purpose:** a new deployable unit. It owns reasoning and formatting; it owns no catalogue
  data and no prices.
- **Callers:** the application, over the agent endpoint (`e04s02`), and the live tier of this epic.
- **Contracts to preserve:** the schema is the **only** artefact shared with the application, and
  `ProductView` remains the only description of a product. The agent echoes SKUs; it never states a
  price.

## Steps

1. Add the agent project to `agentfoundry/AgentFoundry.sln` with a single entry point. → verify: `dotnet build agentfoundry/AgentFoundry.sln -v q --nologo`
2. Write the three contract schemas. → verify: `test -f agentfoundry/shared/contracts/workspace-suggestion.result.schema.json && test -f agentfoundry/shared/contracts/workspace-suggestion.request.schema.json && test -f agentfoundry/shared/contracts/workspace-suggestion.event.schema.json`
3. Build the workflow in Microsoft Agent Framework and wire the verifier into it. The graph runs the
   verifier alone for now: the later nodes attach as they are written, because a stub that approved
   nothing would emit a result a caller could not tell from a real one. →
   verify: `dotnet build agentfoundry/AgentFoundry.sln -v q --nologo`
4. Implement the verifier: classification, the reason-code vocabulary, and the `rejected` result. →
   verify: `dotnet test agentfoundry/tests/AgentFoundry.Tests --nologo --filter "FullyQualifiedName~VerifierTests"`
5. Stream the contract's messages: a stage for each node that finishes, and the typed result at the
   end. The Responses transport itself, and the model behind `IIntentClassifier`, arrive with the
   deployment - the packages are preview and there is no endpoint here to verify them against. →
   verify: `dotnet test agentfoundry/AgentFoundry.sln --nologo`
6. Unit tests AGT-01 … AGT-03 with a scripted model, so no network is used. → verify: `dotnet test agentfoundry/tests/AgentFoundry.Tests --nologo`
7. Record the tracer run under `specs/verifications/e02s02-after.md`. → verify: `test -f specs/verifications/e02s02-after.md`

## Verification Script (Step-by-Step)

1. Run the container locally with a scripted model.
2. Send a workspace-shaped request and confirm the stage events precede one terminal result.
3. Send *"what is the weather in Denpasar"* and confirm `status: rejected` with a reason code and
   `options: []`.
4. Confirm both answers are HTTP 200 — a refusal is a successful classification, not an error.
5. Confirm the result validates against the result schema, and that a hand-edited result that breaks
   the schema is refused.

## Test matrix

| ID | Scenario | Level |
|----|----------|-------|
| AGT-01 | A non-workspace request returns `rejected` with a code and no options | unit |
| AGT-02 | Every message carries `requestId`; the result carries `status` and `attempts` | unit |
| AGT-03 | Stage events arrive in order, and the terminal event carries the result exactly once | unit |

## Out of scope

- The rephraser, suggestor and reviewer (`e02s03` … `e02s05`).
- Deployment (`e02s07`).
- Any application change: the contract is written here, consumed in `e04`.

## Risks

- **The schema is a shared contract with a second solution.** Changing it after `e04` is written is
  the one coordination cost of the two-epic split; the schemas are therefore written first and
  versioned with the container.
- **Prose leaking into the contract.** The temptation is a `rationale` string; the schema forbids it,
  and the application renders copy from `criteria` tokens instead.
- **A refusal that looks like a failure.** HTTP 200 for `rejected` is deliberate and asserted, because
  the caller must distinguish "unrelated request" from "service down".
- **A graph with one node is not a workflow.** What is asserted today is the contract, the stage order
  and the verifier's decision. The loop, the branch and the three remaining nodes are not exercised,
  and `AGT-03` says so out loud rather than implying otherwise.

## Acceptance criteria

- AGT-01 … AGT-03 pass without network access.
- The three schemas exist, and their stage, status and slot vocabularies are asserted against the
  vocabularies in code.
- `dotnet build agentfoundry/AgentFoundry.sln` is clean, and the application's three baselines are
  untouched — re-run and recorded, not assumed.
