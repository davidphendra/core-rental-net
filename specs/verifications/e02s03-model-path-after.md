# e02s03 — the model path, which was recorded as done and was not

**Date:** 2026-09-16
**Branch:** `e03-catalogue-contract`

## What was run

| Check | Before | After |
|---|---|---|
| BUILD `CoreRentalNet.sln` | 0 warnings, 0 errors | **0 warnings, 0 errors** |
| NONBROWSER | 643 passed, 0 failed | **643 passed, 0 failed** |
| `agentfoundry/AgentFoundry.sln` | 38 passed | **72 passed, 0 failed** |

## What this corrects

`e02s03` asks for this, in its own words:

> Only a **miss** reaches the model, and only within the closed `SlotId` enum.

**Task 3 — "Add the miss path: classify into the closed SlotId enum, mark the set inferred, log the
miss" — was closed `passing`, and the story with it.** What existed was the port (`ISlotClassifier`), the
`Inferred` marking and the log line. `ISlotClassifier` was referenced in three places: its own
declaration, `Rephraser`'s constructor, and a doc comment. It was **never implemented and never
registered**, and there was no composition root that could have constructed `Rephraser` at all.

The verification record made it worse rather than better. It reported *"0 model calls"* and *"the
model's answer, all inferred, one miss logged"* — counts of classifier invocations against a test fake,
written in the language of model calls. A reader, including the next me, would reasonably conclude the
model path worked.

`IIntentClassifier` — the workflow's **first** stage — was in the same state.

Nothing external blocked any of this. It was skipped and recorded as done.

## What was built

| Piece | What it is |
|---|---|
| `shared/prompts/workspace-intent.txt` | the verifier: is this a workspace request at all? |
| `shared/prompts/slot-classifier.txt` | which parts does the request mean? |
| `PromptLibrary` | fills a prompt, and **refuses** to render one with an unfilled placeholder |
| `PromptAnswer` | takes the JSON out of an answer, tolerating packaging and refusing its absence |
| `IntentClassifier` | `IIntentClassifier` against `IChatClient` |
| `SlotClassifier` | `ISlotClassifier` against `IChatClient` |

The markers around untrusted text carry a **per-call nonce**. A fixed delimiter is not a delimiter: the
customer's words are the input, so anyone who types the closing marker is outside the block. This is the
delimiter `workspace-suggestions.policy.yaml` has claimed since it was written.

## Three defects found on the way

1. **The verdict's code was written into the result unexamined.** `SuggestionWorkflow` does
   `Code = last.Verdict.Code`, and the result schema's `code` enum holds **exactly one** value. A model
   answering `{"isWorkspaceRequest": false, "code": "too_vague"}` would have produced a result the
   contract forbids — and that schema is the only thing the application and the agent share. The
   classifier now decides the code and never reads the model's, with a test asserting it for four
   different offered codes.
2. **`shared/` never shipped.** No `.csproj` under `agentfoundry/` copied any of it, so
   `IntentTable.Load(path)` had no file to load in a container, and the prompts would have had none to
   send. Both are now `Content` with `CopyToOutputDirectory`.
3. **`PromptAnswer` only understood an object.** The verifier's answer is an object and the classifier's
   is an array; the shared reader forced braces and threw on the array. Found by running it, not by
   reading it.

## The test that was actually missing

Every existing loop test handed the workflow fakes for both ports a model sits behind — which proves the
loop works and says **nothing** about whether the ports can be satisfied. `The_real_classifiers_drive_the_loop`
wires the real classifiers in and counts the model calls: two, one verdict and one classification.

It also caught two of my own mistakes in a row, which is the point of it: a specification missing a
mandatory slot is correctly refused (the run went `exhausted`), and a catalogue holding one product
yields **one** option rather than three, because a run that composed three tiers out of one product
would be padding.

## What is still missing

- **The composition root.** Nothing constructs `Rephraser`, `IntentClassifier` or `SlotClassifier`; a
  container has no `Program.cs`. This is the next piece, and it is what makes the ports reachable in
  a deployment.
- **The hosted agent's own configuration** — endpoint, model deployment and the guardrail attachment —
  which is `e02s07` and needs the subscription.

## The status, left as it is

`e02s03` task 3 stays `passing`, because it is now true. This record is the correction: the task was
passing for a while when it was not, and the reason it went unnoticed is in this file.
