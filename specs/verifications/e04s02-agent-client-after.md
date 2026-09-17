# e04s02 / e04s03 — the agent client, and which method is best practice

**Date:** 2026-09-16
**Branch:** `e03-catalogue-contract`

## What was run

| Check | Before | After |
|---|---|---|
| BUILD `CoreRentalNet.sln` | 0 warnings, 0 errors | **0 warnings, 0 errors** |
| NONBROWSER | 585 passed, 0 failed | **591 passed, 0 failed** |
| BROWSER | 104 passed, 1 skipped | **unchanged** |
| `agentfoundry/AgentFoundry.sln` | 38 passed | **unchanged** |

## The question, and what the source actually says

The sample at `responses/Using-Samples/SimpleAgent/Program.cs` builds one `AIAgent` two ways and the
consuming code cannot tell which:

| | Local | Hosted |
|---|---|---|
| Client | `OpenAIClient` + `Endpoint = http://localhost:8088`, api key `"not-needed"` | `AIProjectClient(project, AzureCliCredential)` |
| Endpoint | `POST /responses` | `{project}/agents/{name}/endpoint/protocols/openai` |
| Guard | none needed | **refuses a bearer on a non-TLS endpoint** |

Four findings decided the method, and two of them contradict what the shape suggests:

1. **`AsAIAgent(Uri)` is not in the released package.** The sample reaches it through a
   `ProjectReference` to the framework's `main`; the released `Microsoft.Agents.AI.Foundry` (preview)
   exposes `AsAIAgent` taking an agent **name**, not the hosted agent's endpoint. The sample is not a
   drop-in.
2. **The OpenAI client's Responses surface is marked evaluation-only** — `OPENAI001`, *"subject to
   change or removal in future updates"*. Microsoft's own sample suppresses it, as does this project's
   Host, in one named place.
3. **The platform already ships a `BearerTokenPolicy`** (`System.ClientModel.Primitives`) — so writing
   one was reimplementing it. It takes an `AuthenticationTokenProvider` rather than a `TokenCredential`,
   and wiring the two is not a documented one-liner, which is why the policy here is thirty lines and
   named for what it is.
4. **There is no built-in client for the Invocations protocol at all** — the sample for it defines its
   own `AIAgent` over `HttpClient`, and that protocol's sample returns a complete response with no SSE.
   For a streaming requirement, Responses is the only protocol of the two with a client and with
   streaming.

## The determination

**The sample's *shape*, on the documented OpenAI-compatible client: one `AIAgent`, two factories, a
local endpoint reached with a placeholder key and a hosted one reached with the application's own
identity.**

| Method | Verdict |
|---|---|
| **`GetResponsesClient().AsAIAgent(...)` + `ApplicationTokenPolicy` + `Azure.Identity`** | **Chosen.** Stable packages; the Responses protocol the platform documents as OpenAI-compatible; the guard and the refresh written deliberately |
| Sample verbatim (`AIProjectClient` + `Microsoft.Agents.AI.Foundry`) | Rejected **on evidence**: its endpoint overload is unreleased, and the packages are beta and preview |
| Hand-written `HttpClient` + SSE | Rejected — owns SSE parsing and token refresh for no gain |

## What changed

- **`IAgentTextStream`** — the agent's answer as text, so the parsing is testable without a client.
- **`FoundryAgentAdapter`** — carries the agent's streamed JSON across the boundary into the
  application's port. **JSONL**: one message per line, terminated; a message the stream never finished
  delivers nothing, because reading a truncated object as though it were whole is how a partial result
  becomes a result.
- **`ResponsesAgentTextStream`** — drives `AIAgent`, **with no session**: one request is one run with no
  memory of any other, which is decision D1 and not the sample's multi-turn REPL.
- **`FoundryAgentSettings`**, **`ApplicationTokenPolicy`**, **`FoundryAgentRegistration`**,
  **`NoAgentConfigured`**.
- Six tests for the adapter, with a hand-written stream fake.

## Measured effect

| | |
|---|---|
| A stage, a result | two messages, in order, the stage first |
| One JSON object split across two updates | read as one |
| A line that is not JSON, or empty | skipped, and the rest still arrives |
| A message the stream never terminated | **not read** |
| No endpoint configured | the port exists and reports `unavailable`; the application starts |

## What the run corrected

1. **The application would not start without an agent.** Registering the port's implementation only
   when an endpoint is configured left `ISuggestWorkspaceOptions` unable to be constructed, and
   thirteen tests caught it at once. `NoAgentConfigured` is a null object: the operation exists
   everywhere, and a deployment without an agent answers "unavailable" rather than failing to start.
2. **`BearerTokenPolicy` collided with the platform's own.** The name was taken, and taking it was a
   signal worth heeding: the class here is `ApplicationTokenPolicy`, named for what it does.
3. **The first `AsAIAgent` attempt did not compile**, which is what exposed finding 1 above — the
   sample's hosted factory is written against the framework's source, not against a release.

## Not done, deliberately

- **The local stand-in process** still arrives with the page that drives it (`e04s03`).
- **No session, anywhere.** The sample's REPL reuses one; ours must not.
