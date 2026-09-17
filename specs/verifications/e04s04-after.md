# e04s04 — four outcomes, one of them ours

**Date:** 2026-09-16
**Branch:** `e03-catalogue-contract`

## What was run

| Check | Before | After |
|---|---|---|
| BUILD `CoreRentalNet.sln` | 0 warnings, 0 errors | **0 warnings, 0 errors** |
| NONBROWSER | 601 passed, 0 failed | **626 passed, 0 failed** |
| BROWSER | 108 passed, 1 skipped | **113 passed, 0 failed, 1 skipped** |
| `agentfoundry/AgentFoundry.sln` | 38 passed | **38 passed** |

## The finding that changed the story

**The agent's contract has three statuses and all of them are answers.** `ok`, `exhausted` and
`rejected`. There is no `unavailable`, so that status belongs to the application alone: it means *no
agent is configured* or *the one that is could not be reached* — which is why it is the only outcome
offering a retry. A refusal and an exhaustion are answers, and the same words would produce the same one.

That is cleaner than what I had assumed, and it came out of reading the schema rather than the code.

## The fixture was speaking a language the contract forbids

My stand-in offered tiers named `essential`, `balanced` and `premium`, criteria of `["desk","chair"]`,
findings of `["budget"]`, and a status of `unavailable`. **The schema allows none of them** — tiers are
`low|middle|high`, criteria must match `^(slot|quantity|tag|attribute):[a-z0-9:.-]+$`, findings are
`{kind, slot}` objects, and the status set is closed at three.

Every test passed. The application faithfully rendered what it was handed, so the suite confirmed that
the page displays *something* while proving nothing about the language it will hear. The fixture has been
rewritten to the contract, and `LocalAgentFixtureTests` now holds every scenario body to the closed
vocabularies — the guard that would have caught it the first time.

The same class of gap is why `ResponsesAgentTextStream` had to grow a translation, below.

## The four outcomes, and how each is said

| Outcome | Rendered | Retry |
|---|---|---|
| `ok` | the candidates | no |
| `exhausted` | the candidates, **selectable**, with a caveat and the findings | no |
| `rejected` | the application's message and guidance | **no** — the same words give the same refusal |
| `unavailable` | the application's error, saying nothing changed | **yes** — this one could differ |

Every sentence is in `OutcomeCopy`, which is the only place a sentence about a run exists. Nothing renders
a raw code: an unrecognised one gets a sentence a person could read, because printing it would look like a
fallback while putting the agent's vocabulary on the page. Tier names are the application's too — the wire
says `low|middle|high`, a customer reads *Essential*, *Balanced*, *Premium* — and a criterion token is
read out as words (`attribute:desk:type:sit-stand` → *desk type sit stand*) because a token on a page is
the agent's vocabulary leaking.

## The count, decided where the identity is read

`CandidateChoices.Visible(options, entitledToThree)` — three for `poweruser:aibuilder`, one otherwise, at
the **middle**: the neutral pick, never the cheapest and never the dearest. With an even count it is the
lower of the two middles, which is still the one that is not the dearest.

Decided before rendering rather than styled away. A candidate that is in the document and hidden is still
a control — reachable by looking, by a screen reader, and by anything that reads the markup — and
`The_unshown_candidates_are_absent_rather_than_hidden` asserts the list that leaves this method is the list
the page has.

## Findings stopped being thrown away

The wire carries `{kind, slot}` and the adapter flattened it to the kind, so the application could say
only that *something* was wrong. It now crosses as `AgentFinding(Kind, Slot)` and reaches the page as the
application's own `SuggestionFinding`, placed on the slot it is about — *"We could not meet every
requirement you gave for the chair."*

## A failure of the client is one failure on the inside

The SDK reports a refused connection, a rejected call and a broken stream as its own exception types. The
application layer now sees one: everything that is not a cancellation leaves the Host adapter as an
`HttpRequestException`, so *"the agent could not be reached"* has a single meaning however many ways the
client finds to say it — and a status the contract does not have can never be mistaken for an answer.
A cancellation passes through as itself, which is the same distinction `e04s03` needed.

This also required splitting `AskAsync` (43 lines, over the budget) so the translation sits outside the
loop that yields: a `yield return` cannot live inside a try with a catch.

## The tests

| Test | What it holds |
|---|---|
| `OutcomeCopyTests` (12) | a known code is the app's words; an **unknown code renders a sentence and never the code**; tiers, criteria and findings read out as words |
| `CandidateCountTests` (5) | three for a power user, the **middle** for anyone else, no padding, and the unshown are absent |
| `AgentClientTests` (+2) | a failing agent becomes `HttpRequestException`; a cancellation does not |
| `LocalAgentFixtureTests` (+1) | every scenario stays inside the contract's closed vocabularies |
| AIB-14 | a refusal: the app's message, no candidates, **no retry**, stages still collapsed |
| AIB-15 | exhaustion: the caveat, the findings placed on the chair, three selectable candidates |
| AIB-16 | a non-power user sees **one**, `data-tier="middle"`, labelled *Balanced* |
| AIB-17 | the customer's own word is quoted back; criteria are words, never `slot:desk` |
| the unreachable agent | the error, nothing applied, and a retry that **works** when the agent comes back |

## Not done, deliberately

- **Applying a candidate** — `e04s05`, and the run guard in `e04s06`.
- **Dropped SKUs are still not rendered.** They cross into `WorkspaceSuggestion` and no surface shows
  them; that belongs with applying a candidate, where a shorter line is something a customer acts on.
