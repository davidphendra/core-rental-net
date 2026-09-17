# e02s03 — the request becomes a specification

**Date:** 2026-09-16
**Branch:** `e03-catalogue-contract`

## What was run

| Check | Before | After |
|---|---|---|
| BUILD `agentfoundry/AgentFoundry.sln` | 0 warnings, 0 errors | **0 warnings, 0 errors** |
| TESTS `agentfoundry/AgentFoundry.sln` | 8 passed | **17 passed, 0 failed** |
| BUILD `CoreRentalNet.sln` | unmoved | **0 warnings, 0 errors — unmoved** |
| NONBROWSER `CoreRentalNet.sln` | 570 passed | **570 passed, 0 failed — unmoved** |

## What changed

- **`shared/intent/workspace-intents.json`** — the declared phrasings, as data with a reviewable diff.
- **`IntentTable`** — the loader and the lookup. Validated on load: a row with no usable phrase, no
  slots, or a slot outside the closed vocabulary is refused **by name**; matching is whole-word,
  case-insensitive, plural-insensitive, and the longest declared phrase wins.
- **`ISlotClassifier`** — the port for a miss, taking the request's slot rules so an answer outside the
  application's vocabulary can be dropped.
- **`Rephraser`** — the table first, the model only on a miss, the miss marked `Inferred` and logged.
- **`QuantityRule`** — a count written in the request honoured, everything else one, clamped to the
  capacity the request carried.
- **`RephraserExecutor`** — the graph's second node, behind a conditional edge off the verifier.
- **`Slots`** and **`QueryText`** — the closed vocabulary in one place, and the normalisation.

## Measured effect

| Request | Result |
|---|---|
| "I would like a full office please" | 6 declared slots, none inferred, **0 model calls** |
| "somewhere to think with a bit of greenery" | the model's answer, all inferred, **one miss logged** |
| "…with two monitors" | monitor quantity 2 |
| "…with five monitors" (capacity 3) | monitor quantity **3** |
| "an office setup" | every quantity 1 |

## What the run corrected

1. **`Specification` as a namespace collides with `Specification` the record.** The same shape of
   mistake as `Workflow` earlier in this epic; the namespace is `Specifications` now, matching
   `Contracts`, `Intent` and `Vocabularies`. Worth naming twice because it is a pattern here: a
   namespace named after the type inside it does not compile.
2. **The intent file had to be an array, and matching had to consider every phrase.** The first draft
   wrapped the rows in an object and matched only the first phrase of each row, which would have made
   the other phrasings decoration. Both were caught by reading the loader against the file.
3. **`AddEdge` needs its message type explicitly.** `AddEdge(verifier, rephraser, condition: …)` cannot
   infer `T` from the condition alone; it is `AddEdge<Verification>(…)`.
4. **A record holding a list is not a value.** `AGT-07` first asserted `first.Should().Be(second)`, and
   failed: record equality compares the list by reference. The assertion is now element by element,
   which is the property that actually matters.
5. **`AGT-07`'s claim was stronger than the code could support.** It said "the slot set is identical
   across all three candidates", and there are no candidates until `e02s04`. It now asserts what is
   true and testable here — one run asks once, and the same request produces the same specification —
   and the cross-tier claim moves to the story that can make it.

## Not done, deliberately

- **No findings use.** The rephraser accepts findings and passes them to the model on the miss path; a
  table hit ignores them, because a table cannot reason about why an option was rejected. The retry
  loop that produces them arrives with the reviewer (`e02s05`).
- **No structured run record.** The miss is logged as a line; the record that reports a miss *rate*
  arrives with observability (`e02s06`).
- **No criteria extraction.** The request's criteria are mapped to the catalogue's vocabulary by the
  suggestor (`e02s04`), which is where the catalogue is known.
