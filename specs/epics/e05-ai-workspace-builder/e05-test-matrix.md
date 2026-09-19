# e05 — AI workspace builder test matrix

Levels:

- `agent` = `agentfoundry` tests against a **fake model client**; no network, no Foundry, no credential.
- `unit` = `tests/CoreRentalNet.Host.Tests` with a hand-written fake suggestion agent.
- `browser` = the Playwright suite against a real application process and the **local stand-in** that
  speaks the same protocol — no mocks, no intercepted calls.
- `eval` = the rubric tier in `agentfoundry/tests/eval`, run deliberately, never in the default job.

| ID | Story | Scenario | Level | Risk |
|----|-------|----------|-------|------|
| AIWB-01 | e05s01 | A serialized result validates against `suggestion.result.schema.json` | agent | P0 |
| AIWB-02 | e05s01 | A request carrying slot rules, capacities and the compact catalogue validates against the request schema | agent | P0 |
| AIWB-03 | e05s01 | Output that cannot be parsed into the contract is refused when the result is read; a violation that parses is the application's to reject | agent | P0 |
| AIWB-04 | e05s01 | The host answers the Responses protocol and streams | agent | P0 |
| AIWB-05 | e05s02 | A vague sentence yields a specification naming only slots that exist | agent | P0 |
| AIWB-06 | e05s02 | The specification carries no SKU, price or product name | agent | P0 |
| AIWB-07 | e05s02 | An off-topic sentence yields the typed not-a-workspace verdict, not an error | agent | P0 |
| AIWB-08 | e05s02 | A stated monthly ceiling survives into the specification; its absence is not invented | agent | P0 |
| AIWB-09 | e05s03 | The workflow runs rephraser then suggestor, in that order, publishing one output | agent | P0 |
| AIWB-10 | e05s03 | The suggestor still sees the catalogue the request carried (default chaining) | agent | P0 |
| AIWB-11 | e05s03 | A slot the request says nothing about may be left empty | agent | P1 |
| AIWB-12 | e05s03 | Both agent identities are stable across a rebuild | agent | P1 |
| AIWB-13 | e05s04 | No raw JSON is ever rendered, at any point of a streamed run | browser | P0 |
| AIWB-14 | e05s04 | A narrative field is hygiened **before** display, including a URL split across chunks | unit | P0 |
| AIWB-15 | e05s04 | A rationale longer than the cap is truncated, and a currency amount is stripped | unit | P1 |
| AIWB-16 | e05s08 | A valid agent result maps to candidates with prices recomputed from the catalogue | unit | P0 |
| AIWB-17 | e05s05 | An unreachable agent yields a stated unavailability, distinct from a refusal | unit | P0 |
| AIWB-18 | e05s05 | With no agent configured the AI section is absent, not open | unit | P0 |
| AIWB-19 | e05s05 | The application's DTOs still match `shared/contracts` | unit | P0 |
| AIWB-20 | e05s06 | An account holding the permission reaches the AI section; one without it does not | unit | P0 |
| AIWB-21 | e05s06 | With the permission's config section unconfigured the AI section is absent, not open | unit | P0 |
| AIWB-22 | e05s06 | A second submit while a run is in flight is refused | unit | P0 |
| AIWB-23 | e05s06 | The guard is released on every exit path, including cancel | unit | P0 |
| AIWB-24 | e05s06 | Regression: the catalogue's own rule still opens when no provider is configured | unit | P0 |
| AIWB-25 | e05s07 | A submitted request shows the model's words as they complete, then candidates | browser | P0 |
| AIWB-26 | e05s07 | No price and no product name appears in streamed text | browser | P0 |
| AIWB-27 | e05s07 | Cancelling is reported as stopped, not as an error, and applies nothing | browser | P0 |
| AIWB-28 | e05s07 | Cancelling leaves the streamed text in place, marked not applied | browser | P1 |
| AIWB-29 | e05s07 | A failed run leaves the streamed text in place, marked not applied, and offers a retry | browser | P0 |
| AIWB-30 | e05s07 | The stage list is retained after the run and collapsed | browser | P1 |
| AIWB-31 | e05s08 | A SKU the catalogue does not hold fails the whole run and says so | unit | P0 |
| AIWB-32 | e05s08 | A quantity of zero **or** over the slot capacity fails the whole run and says so | unit | P0 |
| AIWB-33 | e05s08 | A slot the product does not belong to fails the whole run and says so | unit | P0 |
| AIWB-34 | e05s08 | Candidates are labelled by rank: three, two and one are each labelled correctly | unit | P0 |
| AIWB-35 | e05s08 | Options closer than the configured 1.5x spread are refused, and fewer are shown rather than padded | unit | P0 |
| AIWB-36 | e05s08 | Choosing a candidate on a non-empty workspace confirms, then replaces the composition | browser | P0 |
| AIWB-37 | e05s08 | Confirming leaves the delivery address unchanged | browser | P0 |
| AIWB-38 | e05s08 | A version conflict applies nothing and says the workspace changed | unit | P0 |
| AIWB-39 | e05s09 | A run record carries tokens, model, prompt version, model calls, payload hash, the **raw model output** and an opaque id — and no PII | unit | P0 |
| AIWB-40 | e05s09 | Only the query and the catalogue cross to Foundry — no identity, address or draft state | unit | P0 |
| AIWB-41 | e05s09 | With the feature flag off the panel is absent and the endpoint answers unavailable | browser | P0 |
| AIWB-42 | e05s09 | The golden set passes the deterministic tier offline | agent | P0 |
| AIWB-43 | e05s09 | The rubric tier meets its threshold against `gpt-4.1-mini` on a standard deployment | eval | P0 |
| AIWB-44 | e05s07 | The AI section sits above the canvas and is collapsed to one line when idle | browser | P1 |
| AIWB-45 | e05s07 | The section expands while a run is in flight and stays expanded while candidates are shown | browser | P1 |
| AIWB-46 | e05s07 | On a narrow viewport the section is a full-width card above the canvas | browser | P1 |
| AIWB-47 | e05s05 | No session is carried into a later run: two identical runs send identical payloads | unit | P0 |
| AIWB-48 | e05s01 | `gpt-4.1-mini` returns a typed result on the **Responses** path, and the model call count is recorded | eval | P0 |
| AIWB-49 | e05s07 | Reloading or navigating away mid-run cancels it, applies nothing, and leaves the section idle | browser | P1 |
| AIWB-50 | e05s07 | The AI section appears on the builder and on no other page | browser | P1 |
| AIWB-51 | e05s09 | A run exceeding the 45 s hard timeout fails visibly, applies nothing, and is recorded as timed out | unit | P1 |
| AIWB-52 | e05s07 | The streamed prose is not a live region, while the stage lines and the candidates are announced politely | browser | P1 |

## Baselines

Record the current numbers at kickoff.

| Baseline | Requirement |
|---|---|
| BUILD | `dotnet build CoreRentalNet.sln` and `dotnet build agentfoundry/AgentFoundry.sln`, both clean |
| NONBROWSER | every non-browser project green and **100% offline** — no Foundry, no credential, no network |
| BROWSER | the Playwright suite green against the local stand-in |
| AGENT | `dotnet test agentfoundry/AgentFoundry.sln` green against the fake model client |
| COST | tokens and model-call count recorded for the golden set, before any cap is set |
