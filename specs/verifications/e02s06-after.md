# e02s06 — the run is measurable, and the catalogue is data

**Date:** 2026-09-16
**Branch:** `e03-catalogue-contract`

## What was run

| Check | Before | After |
|---|---|---|
| BUILD `agentfoundry/AgentFoundry.sln` | 0 warnings, 0 errors | **0 warnings, 0 errors** |
| TESTS `agentfoundry/AgentFoundry.sln` | 34 passed | **38 passed, 0 failed** |
| BUILD `CoreRentalNet.sln` | unmoved | **0 warnings, 0 errors — unmoved** |
| NONBROWSER `CoreRentalNet.sln` | 570 passed | **570 passed, 0 failed — unmoved** |

## What changed

- **`Observability/RunRecord`** — outcome, attempts, request **length**, whether the slot set was
  inferred, the finding count and the catalogue read count.
- **`Observability/RunLog`** — one line per run on every path out, including a refusal, and the
  `ActivitySource` the run's spans are published under.
- **`SuggestionWorkflow`** — one span per run with an event per stage, so a run that spent three
  attempts is visible at a glance; and the record assembled in one place.
- **`ISelectCandidates.CatalogueReads`** — the suggestor reports how many times it read, because a
  second read is an authenticated round trip for data already in hand.
- **`shared/guardrails/workspace-suggestions.policy.yaml`** — the content policy written down where it
  can be reviewed.

## Measured effect

| | |
|---|---|
| A run that was inferred from the table missing | `ended ok after 1 attempt(s): slots inferred=True, findings=0, catalogue reads=1` |
| The request in the log | **its length, never its words** — a request is a person's own text |
| Catalogue text in the contract | **none**: the contract carries SKUs, slots and tokens, so a product description has no field to reach a caller through |
| An instruction written into a product name | changes no composition, and reaches no message |

## What the run corrected

1. **Counting reads belongs with the per-run suggestor, not a decorator.** The first draft wrapped the
   reader in a counting decorator, which would have counted across every run in the container's lifetime
   — a number that grows all day and means nothing. The suggestor is per run, so the count is.
2. **The tracer field leaked into the class.** A local list meant for one assertion was written as a
   static field, which the repository forbids outright: no mutable static state. It is a local again.

## Deliberately not done

- **No exporter.** The spans are published on an `ActivitySource`; where they go is deployment
  configuration and arrives with `e02s07`. Nothing here needs an OpenTelemetry package to be useful: a
  run's shape is asserted through the framework's own listener.
- **No prompt delimiters, because there are no prompts.** The story's third step was to delimit
  catalogue content as data wherever it reaches a prompt. The model adapters are still ports, so there
  is no prompt to delimit. What is asserted instead is the **structural** form of the same rule, which
  is stronger: catalogue text never crosses the contract at all, so there is no field an injection could
  travel in even if a later prompt were built carelessly. The delimiter rule belongs with the adapter
  that writes one.
- **No metric export or dashboards.** The line and the span are the substrate; reading them is a
  deployment concern.
