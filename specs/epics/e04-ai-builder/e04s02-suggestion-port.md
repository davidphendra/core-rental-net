# e04s02 — The application can ask the agent and understand the answer

**type:** feat
**risk:** P0
**context:** application
**bcps:** 5
**status:** in-progress

## Context

The application needs a port it can depend on, an adapter that calls the hosted agent, and a **real
local stand-in** so the browser suite stays hermetic. Everything the page needs to know about an
answer is decided here — including the two rules that make the feature safe: **prices are recomputed
from the catalogue and never taken from the agent**, and **a SKU the catalogue does not hold never
reaches the page**.

## Requirements

#### ADDED: the port and its rule

`ISuggestWorkspaceOptions` — one public operation — takes the request and returns an outcome. Its
`sealed` implementation holds the rules:

- a SKU the catalogue does not hold is **dropped**, and the drop is reported;
- **fewer than three valid candidates is stated as fewer**, never padded;
- an **unreachable agent** yields a stated unavailability, distinct from a refusal;
- prices and names come from the catalogue, never from the agent's answer.

#### ADDED: the adapter, and the local stand-in

The Host adapter calls the agent over its OpenAI-compatible endpoint with the **application's own
identity** (`AzureCliCredential` locally, a managed identity when deployed) and streams stage events.
The agent endpoint, agent name and model deployment name are **configuration**; the adapter is
registered only when the endpoint is configured.

`tests/CoreRentalNet.E2E.LocalAgent` is a **real process** that serves the same shapes, selectable per
scenario so it can emit `ok`, `rejected`, `exhausted`, a mid-stream disconnect and a slow run —
mirroring `LocalProvider`, and keeping the browser suite's no-interception rule intact.

## Zoom-Out

- **Module purpose:** the Workspace application owns a customer's draft; this port proposes contents
  for it. It never becomes a second source of prices or products.
- **Callers:** the builder page, through the published contract.
- **Contracts to preserve:** `ProductView` as the only description of a product; the Application
  layer's independence from ASP.NET (the agent call lives in the Host); the suggestion contract
  written by `e02s02`.

## Steps

1. Add the request and outcome records to the Workspace application's contracts. → verify: `dotnet build CoreRentalNet.sln -v q --nologo`
2. Add `ISuggestWorkspaceOptions` and its `sealed` implementation with the four rules. → verify: `dotnet test tests/CoreRentalNet.Host.Tests --nologo --filter "FullyQualifiedName~WorkspaceSuggestionTests"`
3. Add the Host adapter calling the agent with the application's own identity, on the
   OpenAI-compatible client the Responses protocol documents, registered always so the operation's
   dependencies are complete. → verify: `dotnet test tests/CoreRentalNet.Host.Tests --nologo --filter "FullyQualifiedName~FoundryAgentAdapterTests"`
4. Add the local stand-in process with its scenario switch. **Deferred to `e04s03`**: the browser tier
   that drives it arrives with the page, and a scenario-selectable server built before its consumer means
   inventing the scenarios. → verify: `dotnet build tests/CoreRentalNet.E2E.LocalAgent -v q --nologo`
5. Unit tests AIB-06 … AIB-10 with a hand-written fake, no mocking library. → verify: `dotnet test tests/CoreRentalNet.Modules.Workspace.UnitTests --nologo --filter "FullyQualifiedName~SuggestionTests"`
6. Record the stand-in's contract in `specs/verifications/e04s02-after.md`. → verify: `test -f specs/verifications/e04s02-after.md`

## Verification Script (Step-by-Step)

1. Run the local stand-in and call the port through the page's path → three candidates, each priced by
   the catalogue.
2. Point the stand-in at a result naming an unknown SKU → the line is dropped and reported.
3. Point it at a result with two candidates → two are shown and the count is stated.
4. Stop the stand-in → the outcome is "unavailable", not "refused".
5. Confirm no run needs a Foundry credential, an `az login`, or the network.

## Test matrix

| ID | Scenario | Level |
|----|----------|-------|
| AIB-06 | A valid result maps to candidates with catalogue prices | unit |
| AIB-07 | An unknown SKU is dropped and reported | unit |
| AIB-08 | Fewer than three is stated, never padded | unit |
| AIB-09 | An unreachable agent is stated as unavailable | unit |
| AIB-10 | Refusal and transport failure are different outcomes | unit |

## Out of scope

- The page and its streaming presentation (`e04s03`).
- Rendering the outcomes (`e04s04`) and the apply path (`e04s05`).
- Any change to the agent itself (`e02`).

## Risks

- **Trusting the agent's prices.** The agent is never a source of money; the port recomputes from the
  catalogue, exactly as the draft does.
- **A stand-in that drifts from the real contract.** It is written against the schema, and the live
  tier asserts the same shapes — but the schema is the thing to change first, never the stand-in alone.
- **Layering.** The agent call belongs in the Host; the rules belong in the application. An HTTP client
  in the Application layer is the mistake the architecture tests exist to catch.
- **The identity leaking into tests.** The hermetic tier must never need a credential; if it does, the
  stand-in was bypassed.

## Acceptance criteria

- AIB-06 … AIB-10 pass with no network and no credential.
- No candidate can reach the page carrying a SKU the catalogue does not hold.
- The browser suite can run the whole feature against the stand-in.
