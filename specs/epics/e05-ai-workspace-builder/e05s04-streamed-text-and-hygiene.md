# e05s04 — The customer reads the model's words, never its JSON

**type:** feat
**risk:** P0
**context:** app
**bcps:** 5
**status:** passing

## Context

The customer watches the run. The agent produces **structured JSON**, so what arrives at the
application is braces, keys and escaped quotes — and the application is also where the display is, so
the application is where that stream has to become readable prose.

It is deliberately not "stream every token": revealing a narrative field only when its JSON string
**closes** is what lets hygiene run on a **complete** value. Token streaming would make hygiene
best-effort — a URL split across two chunks would escape a strip.

Because this work is here, the agent holds **no customer-facing code at all**: it has no `Streaming/`
and no `Guardrails/`, and its whole job is payload in, structured result out.

## Requirements

#### ADDED: the incremental reader

`AiBuilder/NarrativeFieldReader` consumes the adapter's stream, watches for completed string values, and
emits each narrative field the moment it closes. **Raw JSON is never rendered, and no partial value is
rendered either.**

#### ADDED: hygiene before display

`AiBuilder/OutputHygiene` caps the rationale at **280** characters and each line's `why` at **140**, and
strips URLs, markup and currency amounts, applied to each completed field **before** it reaches the SSE
writer. It is an ordinary class
the endpoint calls — not a framework middleware — because it runs where display happens, not somewhere
else that has to be trusted to have run.

#### ADDED: purpose-only text, and the prompt that makes it true

The streamed text may not contain a price or a product name. The **prompts forbid both** — that
obligation belongs to `e05s02` and `e05s03` — and the strip here is the deterministic backstop. **This
is what makes showing text before validation acceptable**: the customer reads a description of an idea,
not an offer.

#### ADDED: the refusal shape

The typed not-a-workspace verdict stays a **result** in the contract, not an exception and not prose, so
the application words it (`e05s07`).

#### NOT ADDED, and why

No prompt-shield or moderation pass. The agents are tool-less and the application validates every SKU
and quantity, so an extra call buys little until the run records (`e05s09`) show abuse. That upgrade
path is recorded, not taken.

## Zoom-Out

- **Module purpose:** the application's rendering of the model's stream — parsing and hygiene, nothing
  else. It alters no SKU, no quantity and no slot.
- **Callers:** the streamed endpoint (`e05s07`).
- **Contracts to preserve:** no raw JSON reaches a customer at any point; hygiene touches free text only.

## Steps

1. Add the incremental reader and assert no raw JSON is emitted at any point of a stream.
   → verify: `dotnet test tests/CoreRentalNet.Host.Tests --nologo --filter "FullyQualifiedName~NarrativeFieldReaderTests"`
2. Apply hygiene to each completed field before it reaches the SSE writer.
   → verify: `dotnet test tests/CoreRentalNet.Host.Tests --nologo --filter "FullyQualifiedName~OutputHygieneTests"`
3. Keep the refusal a typed result in the contract, and run AIWB-13 to AIWB-15 including the
   URL-split-across-chunks case.
   → verify: `dotnet test tests/CoreRentalNet.Host.Tests --nologo`

## Outcome

Built and verified offline: `AiBuilder/NarrativeFieldKind`, `NarrativeField`, `NarrativeFieldReader`,
`OutputHygiene`; `NarrativeFieldReaderTests` and `OutputHygieneTests` — 19 tests. Nothing was deployed and
nothing is assumed.

**The wire was captured rather than assumed, and it corrected the design twice.** A throwaway probe started
the real host over the real workflow with the scripted model client and dumped the SSE, then was deleted.
It established:

- The stream carries **two top-level objects** — the rephraser's specification first, the suggestor's
  result last — as `response.output_text.delta` events on **separate message items** (`item_id`,
  `output_index` 3 then 10). The boundary between the two agents' answers is explicit on the wire.
- Because the answer is a sequence of values rather than one document, the reader parses with
  `AllowMultipleValues` and `isFinalBlock: false`. Verified separately: that combination stops **cleanly**
  at an incomplete token — mid-string, mid-escape, mid-object — and never throws for truncation, so
  re-scanning from the start is obviously correct and cheap enough that resuming mid-token is not worth
  the bookkeeping.
- `workflow_action` items with `kind: "InvokeExecutor"` and `action_id` `rephraser_<hex>` /
  `suggestor_<hex>`, `status` moving `in_progress` → `completed`, are on the wire. **These are the stage
  identities `e05s07` needs, and whether MAF's `FoundryAgent` surfaces them to the application is NOT known
  — recorded as a risk on `e05s07`, not assumed either way.**

**Two things the tests changed.** A tag replaced by a space leaves it stranded before a full stop
(`"for two ."`), so `OutputHygiene` removes a space before punctuation; and the first draft of the
"value not yet closed" test was wrong, not the code — the line's `why` closes before the rationale does.

The real captured text is the test fixture, fed one character at a time, so a chunk boundary falls inside
every escape and every URL the reader will meet.
