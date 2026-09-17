# e04s04 — Every outcome is said in the application's own words

**type:** feat
**risk:** P0
**context:** ui
**bcps:** 5
**status:** failing

## Context

A run ends in one of **four** ways, not two, and only codes cross the boundary from the agent — **no
model-generated prose is ever displayed**. That is what makes it impossible for text derived from a
query or from catalogue content to become part of the interface, and it matches the rule the workspace
already follows: one text in one place, *"so every control that shuts the way out says the same thing"*.

| Outcome | What the customer sees |
|---|---|
| `ok` | the candidates |
| `rejected` | the application's message and guidance — **no retry**, because the same text gives the same answer |
| `exhausted` | the candidates, **selectable**, with a caveat and the outstanding findings |
| transport failure | the application's error and a **retry** — unlike a refusal, a retry can succeed |

## Requirements

#### ADDED: the four rendered outcomes

The Host owns the copy: a reason code maps to a sentence, an exhaustion to a caveat, a transport
failure to an error and a retry control. The stage list stays collapsed so the customer can see where
the run stopped.

#### ADDED: the entitlement decides how many candidates are shown

`poweruser:aibuilder` decides **three**; any other entitled account is shown **one**, and it is the
**middle** candidate — the neutral choice, neither the cheapest available nor the most expensive.
Truncation happens in the Host, where the identity is read, and never by hiding markup.

#### ADDED: criteria the catalogue could not express are shown

Each candidate carries the criteria it satisfied and any it could not, and the customer is told which
— *the word the customer used*, quoted, so nothing the model wrote is displayed as copy.

## Zoom-Out

- **Module purpose:** the section reports what happened and what can be done next; it decides nothing
  about entitlement beyond the count it renders.
- **Callers:** the builder page and the browser suite.
- **Contracts to preserve:** the four outcome shapes from `e02s02`; the rule that only codes and the
  customer's own phrase cross the boundary.

## Steps

1. Add the outcome records for refusal, exhaustion and transport failure, distinct from the ok path. →
   verify: `dotnet build CoreRentalNet.sln -v q --nologo`
2. Add the Host's code-to-copy map, with an unknown code rendering a generic message and **never** the
   raw code. → verify: `dotnet test tests/CoreRentalNet.Host.Tests --nologo --filter "FullyQualifiedName~OutcomeCopyTests"`
3. Render a refusal with guidance and no retry. → verify: `dotnet build CoreRentalNet.sln -v q --nologo`
4. Render exhaustion with the candidates, the caveat and the findings. → verify: `dotnet build CoreRentalNet.sln -v q --nologo`
5. Render a transport failure with a retry control. → verify: `dotnet build CoreRentalNet.sln -v q --nologo`
6. Truncate to one candidate in the Host for a non-power-user, choosing the middle one. → verify: `dotnet test tests/CoreRentalNet.Host.Tests --nologo --filter "FullyQualifiedName~CandidateCountTests"`
7. Render the met and unmet criteria on each candidate. → verify: `dotnet build CoreRentalNet.sln -v q --nologo`
8. Browser tests AIB-14 … AIB-17. → verify: `dotnet test tests/CoreRentalNet.E2E --nologo --filter "FullyQualifiedName~AiBuilderOutcomeTests"`

## Verification Script (Step-by-Step)

1. Stand-in in its refusal scenario → the application's message, no candidates, no retry.
2. Stand-in in its exhaustion scenario → three candidates with the caveat and the findings, and they
   can be selected.
3. Stand-in in its `ok` scenario as a non-power-user → exactly one candidate, and it is the middle one.
4. Same run as a power-user → three candidates.
5. Stand-in stopped mid-run → the error and a working retry.
6. A request containing a word the catalogue cannot express → the word is quoted back as unevaluated,
   and no other model text appears.

## Test matrix

| ID | Scenario | Level |
|----|----------|-------|
| AIB-14 | A refusal shows the application's message and no candidates | browser |
| AIB-15 | Exhaustion shows its candidates with the caveat | browser |
| AIB-16 | A non-power-user is shown one candidate, the middle one | browser |
| AIB-17 | Unevaluated criteria are shown on the candidate | browser |

## Out of scope

- The streaming presentation and cancel (`e04s03`).
- Applying a candidate (`e04s05`).
- Any change to the agent's reason-code vocabulary (`e02s02`), which is versioned with the container.

## Risks

- **Rendering the agent's message.** The schema forbids prose and the Host maps codes, so there is no
  channel — but a fallback that prints an unknown code would be one. The fallback is generic copy.
- **Offering a retry for a refusal.** Retrying identical text produces an identical refusal; the
  guidance is what helps.
- **Truncating in the markup.** The count is decided in the Host; a hidden candidate is not a control.
- **`exhausted` reading as failure.** It is a result with a caveat, and its candidates are selectable.

## Acceptance criteria

- AIB-14 … AIB-17 pass against the local stand-in.
- No model-generated sentence is rendered; an unknown code renders generic copy.
- A non-power-user sees exactly one candidate and it is the middle one.
- The three baselines are green.
