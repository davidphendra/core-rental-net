# e02s04 — the catalogue reader, and why the composition root is not here yet

**Date:** 2026-09-16
**Branch:** `e03-catalogue-contract`

## What was run

| Check | Before | After |
|---|---|---|
| BUILD `CoreRentalNet.sln` | 0 warnings, 0 errors | **0 warnings, 0 errors** |
| NONBROWSER | 643 passed, 0 failed | **643 passed, 0 failed** |
| `agentfoundry/AgentFoundry.sln` | 72 passed | **80 passed, 0 failed** |

## Why this is the reader and not the root

The task was the composition root. Asked to wire the graph, I checked what the graph would resolve to —
and `ICatalogueReader` **had no implementation**. The agent's only way to read the catalogue did not
exist, so a root would have registered a port nothing satisfied, which is a start-up failure dressed as
progress.

The reader is also the piece that was **not** blocked. Which grant gets the token — the platform's
toolbox or the container's own identity — is `e02s01`'s decision and needs a subscription. A reader that
**takes the token as a port** is the same either way, so the blocked decision stops blocking the reader.

## What was built

`ICatalogueAccessToken` — the token, asked for **per read** rather than held. An access token expires,
and a value captured at start-up is how a container stops seeing the catalogue hours later for a reason
nothing connects to the token.

`HttpCatalogueReader` — one unfiltered `view=compact` request per run, deserialised **straight into this
module's records**. They agree field for field with the application's compact projection, which is the
point of a shared contract; a translation layer between two identical shapes is where a rename goes
unnoticed.

## The finding the tests made

The reader originally refused a truncated page on the application's `truncated` flag alone. Writing the
test exposed that the agent's `CataloguePage` carries **`Truncated` as a field** while the application
**computes** it from the two counts it also sends — so the two signals can disagree, and the day the
computed one stopped being published the flag would read `false` on a page that was half missing.

It now refuses on **either** reading: the flag, and `count < total`. A truncated page is the one thing
worse than no page — a partial catalogue that looks complete, from which a slot it happens to be missing
would read as a slot the catalogue cannot fill, and the run would be confidently wrong.

## Still missing, and now precisely

| The graph needs | Who owns it |
|---|---|
| `IIntentClassifier`, `ISlotClassifier` | **done** — this and the previous record |
| `IRephraseRequests`, `ISelectCandidates`, `IReviewComposition` | **done** — `e02s03`–`e02s05` |
| `ICatalogueReader` | **done** — this record |
| `ICatalogueAccessToken` | `e02s01` decides the grant; the deployment implements it |
| `IChatClient` | `e02s07` — endpoint, deployment name, and the guardrail attachment |
| the protocol surface the platform calls in on | `e02s07` |
| **the root that wires them** | **next, and now one step away** |

So the composition root is no longer blocked by a missing implementation — only by the two things that
genuinely need the subscription. It can be written with those two supplied from configuration, which is
the same shape the application's own `FoundryAgentRegistration` already has.
