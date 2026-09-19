# e05s07 — The customer sees the run happen, and can stop it

**type:** feat
**risk:** P0
**context:** app
**bcps:** 5
**status:** in-progress

## Context

A run carries ~172k input tokens, so the wait is real and must be filled honestly. The customer reads
the model's own words as they are produced; the words around them — the stages — are the application's.
A blank wait would make a normal run feel broken, and a failure that says only "it did not work" cannot
be acted on.

## Requirements

#### ADDED: the streamed endpoint

`POST /api/builder/suggest`, declared as `BuilderRoutes.Suggest` beside `CatalogRoutes`, returning
**SSE** to the browser. Implemented as a minimal-API endpoint rather than a controller: a streaming
response is not what an MVC action is shaped for, even though the catalogue API is a controller.

The application writes a short, app-owned sequence — *Reading your request → Matching the catalogue →
Checking the suggestion* — and forwards the model's narrative fields as they complete (`e05s04`). The
browser never talks to Foundry.

> **Gated by `AiPolicy.Name`, and this is not optional.** `e05s06` built the permission and left this open
> deliberately: **hiding a section is not authorisation**, and this endpoint is where the money is spent.
> It must carry the AI policy itself — `[Authorize(Policy = AiPolicy.Name)]`, or the minimal-API
equivalent — so that a caller who never sees the section still cannot run one. The architecture test that
> asserts the catalogue controller carries its own policy is the shape to copy here.

#### ADDED: the AI section on the builder page

The section sits at the **top of the centre column, above the canvas** — it is the region the suggestion
rewrites, so it belongs beside the thing it changes, not in the product panel, which on a phone becomes
a row of chips and would hide it.

- **Builder only.** It appears on `/builder` — the page that draws the composition it replaces — and
  nowhere else. Surfacing it where the canvas is absent would invite a customer to apply a setup they
  cannot see.
- **Collapsed to one line when idle**: a field and a Suggest control. It expands only while a run is in
  flight or while candidates are present.
- **One region, never a modal.** A dialog would hide the canvas the customer is about to have replaced.
- **On a phone** it is a full-width card above the canvas; product selection keeps its existing
  behaviour.
- **Absent entirely** when the feature is off or the customer lacks the permission, so the page looks
  exactly as it does today.

The states it must render, each reachable and each to be tested: idle (collapsed) · running (stages,
streamed text, Stop) · candidates (one to three, labelled by rank, each with Apply) · confirming ·
applied · failed (streamed text kept and marked not applied, with Retry) · stopped (neutral, with
Start again).

#### ADDED: what assistive technology hears

The streamed prose is **not** a live region. It is commentary, and announcing it field by field would talk
over the whole run; a growing `role="status"` region would flood. Instead the short app-owned **stage
lines** go into a polite `role="status"` region — matching the alerts the application already uses — and
the **candidates** announce as the terminal outcome.

So a screen-reader customer hears progress and the result, never a monologue, and is never left in silence
during a run that can reach 45 s.

#### ADDED: the atomic result

The structured result arrives whole and is validated as a whole (`e05s08`) before a single candidate is
rendered. Stage copy is the application's; the agent's stage identities are mapped, never forwarded.

> **RISK, from evidence captured at `e05s04` — and RESOLVED at `e05s07` by reading the shipped packages,
> not by guessing.** The stage identities *are* on the wire: `workflow_action` items with `kind:
> "InvokeExecutor"` and `action_id` `rephraser_<hex>` / `suggestor_<hex>`, moving `in_progress` →
> `completed`. **They cannot reach this application.** Those literals appear in exactly one assembly on
> the machine — `Azure.AI.AgentServer.Responses`, the server side — and in none of the client packages the
> application references. `Microsoft.Agents.AI.Foundry` has no type for the item, its response-item kind
> discriminator matches a single value (`"message"`), and its content union is closed
> (`WireTextContent`, `WireImageContent`, `WireToolCallContent`, `WireToolResultContent`). An
> `AgentResponseUpdate` therefore has nothing on it that could name an executor, and the adapter reads its
> `Text` in any case.
>
> **So the stages are not derived from the agent, and the earlier design of mapping agent stage ids is
> dropped rather than guessed at.** The sequence below is what the application knows on its own — the
> sentence it read, the catalogue it matched, the answer it is checking — and each line is written at the
> moment that phase actually happens. The two-message-item fallback is not needed either: the app-owned
> sequence requires no signal from the wire at all. What remains of the original requirement is its point —
> **the agent's vocabulary never reaches the customer**, which is now true by construction rather than by
> a mapping that has to be maintained.

#### ADDED: cancellation, and what it means

Aborting the stream cancels the agent call through the request's `CancellationToken`. Cancelling is a
**neutral *stopped* state** — the customer chose to stop, so it is not reported as an error — it applies
nothing, and it releases the run guard (`e05s06`). The run record logs it as cancelled.

**A dropped connection is a cancel.** Reloading the page or navigating away mid-run cancels the run
exactly as Stop does: nothing is applied, the guard is released, and on return the section is idle. There
is no server-side run to recover and no run id to retrieve — the customer asks again. This keeps one rule
rather than a second concurrency semantic, and it is the behaviour to test, not to hope for.

#### ADDED: keeping the text, and the failure vocabulary

Streamed text is **kept and marked not applied** on both failure and cancel; it is never retracted
mid-read. Because the text carries no price and no product name (`e05s04`), leaving it on screen cannot
mislead. Every failure ends the run, says so, and offers a retry: *unavailable* (no agent, identity,
transport, timeout), *invalid* (schema or validation), and *refused* (the typed verdict, which is not an
error). **There is no fallback suggestion.**

The wording is the application's, and it is the only place the difference between an outcome and a
failure is visible:

- **refused** — *"That doesn't look like a workspace request — try describing the room, the work, or a
  budget."* It does not blame the customer, because it is a result, not an error.
- **failed** — *"This suggestion couldn't be completed. Nothing was applied."* with **Try again**.
- **stopped** — *"Stopped. Nothing was applied."* with **Start again**.

#### ADDED: the stage list after the run

Stages are **retained and collapsed**, not removed, so a customer or a support engineer can see where a
slow or partially failed run went.

## Zoom-Out

- **Module purpose:** presentation of a run. It owns the words and the lifecycle, not the reasoning.
- **Callers:** the AI section in the builder page.
- **Contracts to preserve:** no raw JSON and no unvalidated candidate is ever rendered; a cancelled or
  failed run applies nothing.

## Steps

1. Add the SSE endpoint writing app-owned stages, with the run guard around it and `AiPolicy.Name` on it.
   → verify: `dotnet build CoreRentalNet.sln -v q --nologo`
2. Forward the model's narrative fields as they complete; keep the result atomic.
   → verify: `dotnet test tests/CoreRentalNet.Host.Tests --nologo --filter "FullyQualifiedName~SuggestionStageCopyTests"`
3. Wire cancellation through, report it as stopped, and release the guard on every exit path.
   → verify: `dotnet test tests/CoreRentalNet.Host.Tests --nologo`
4. Place the section above the canvas: collapsed when idle, expanded while running or showing
   candidates, full-width on a phone, and absent when the feature is off or the permission is missing.
   → verify: `dotnet test tests/CoreRentalNet.E2E --nologo --filter "FullyQualifiedName~AiBuilderSectionTests"`
5. Browser tests AIWB-25 to AIWB-30 and AIWB-44 to AIWB-46 against the local stand-in.
   → verify: `dotnet test tests/CoreRentalNet.E2E --nologo --filter "FullyQualifiedName~AiBuilderStreamTests"`
