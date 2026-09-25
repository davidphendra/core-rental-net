# 0007 — The run is neither limited per customer nor checked by the application

**Status:** accepted
**Date:** 2026-09-25
**Deciders:** product owner, engineer

## Context

`BuilderController` — the run endpoint [0006](0006-the-run-endpoint-is-a-controller.md) created — depended on
seven things. The product owner asked for three of them to go: `RunGuard` ("useless for now"),
`SuggestionValidator` ("we let LLM do the validation") and `IOpaqueTokenService` ("we already had Authorization
attribute, so assume the access token is valid").

Reading the code first changed what each removal means, because only one of the three is what its name suggests
in this controller:

1. **`RunGuard`** is not idle machinery. Its own remark calls a run *"paid and abusable"*, and it is the **only**
   server-side bound on how many runs one customer can have in flight — deliberately not a numeric cap, because
   a placeholder "would look tuned without being so". `RunLease`, `ApiErrorCode.RunInFlight`
   (`"builder.run_in_flight"`, a published contract code) and the `409` branch exist only to serve it.
2. **`SuggestionValidator`** is not "validation by the model instead of by us". It **re-prices from the
   catalogue**: `catalogue.Find(line.Sku)`, then the name and `product.MonthlyPrice.Amount * line.Quantity`. It
   refuses the whole run when the SKU is unknown, the quantity is below one, the quantity exceeds the slot's
   capacity, or the product belongs in a different slot; and `SuggestionSpread` — reachable only through it —
   decides whether options far enough apart to be shown as a range. It had **no test of its own**.
3. **`IOpaqueTokenService` cannot be removed from the application.** It is used by `DraftTokenMiddleware`,
   `PlaceOrderService` and ten Workspace/Rentals handlers. In this controller it did exactly one thing —
   `tokens.HashOf(CustomerKey.Of(User) ?? "anonymous")` — to give `AiRunRecord.CustomerId` an opaque value. It
   has nothing to do with the access token, so the reason given does not attach to it.

The access token is a **separate** value, and the reason given for dropping it does not survive contact with the
agent: `[Authorize(Policy = AIUse)]` decides whether the **browser** may start a run; the access token is read by
`HttpContext.GetAccessTokenAsync()`, sent to the agent as `SuggestionRequest.McpAccessToken`, extracted by
`SuggestionRequestSplitter` before the model can see it, and attached as a Bearer header by
`AuthorizationBearerHandler` for the agent's own calls **back** to this Host's `/mcp`. The agent's
`McpAccessTokenService` holds **no fallback**:

> `throw new InvalidOperationException("The request carried no MCP access token.")`
> *"Replaces the client-credentials exchange: the credential is the caller's."*

Dropping the read would not "assume the token is valid" — it would mean every run's catalogue search fails
before the first tool call, on a token the attribute above never sees.

## Decision

**D1 — `RunGuard` and `RunLease` are removed, with the `409` and its code.** Nothing now bounds a customer's
concurrent paid runs on the server. Accepted knowingly: the page still stops its own previous run
(`ai-run.js` calls `stop()` before `start`), so the UI is unchanged, and what is given up is the server-side
enforcement for a caller that is not the page. `ApiErrorCode.RunInFlight` goes with it — leaving a contract code
for a status this API can no longer return would be a published lie.

**D2 — `SuggestionValidator` and `SuggestionSpread` are removed.** The names and the amounts shown are the
agent's, stated from the catalogue tool's own answers; the application only adds the lines up. The four checks
do not disappear, they **move**: applying a candidate writes a composition command, and the module refuses an
unknown SKU, a quantity over capacity, or a product in the wrong slot. So a candidate that cannot be honoured
now fails **when the customer applies it** rather than during the run. `AgentFoundry:SpreadFactor` is removed
with the type that read it.

**D3 — The empty-answer guard is kept.** An answer whose status claims a suggestion and carries no option is
still refused as `failed: invalid`, because there is nothing to present and an empty candidate list is not a
suggestion. That keeps `AiRunVerdict.Invalid` and `SuggestionEventStream.Invalid` meaningful, and it preserves
the existing handling of `catalogueUnavailable` unchanged.

**D4 — The `Checking the suggestion` stage is removed.** It is application-authored copy that told the customer
their answer was being checked. Nothing checks it now, so the line would be false; deleting a false statement is
better than inventing a replacement. The sequence is `Reading your request` → `Matching the catalogue`, and the
model's own words follow.

**D5 — `IOpaqueTokenService` leaves the controller; the record keeps its opaque id.** The dependency is gone and
the class stays where it belongs (tokens address a draft or an order). The hash moved to
`CustomerKey.HashedOf` — plain SHA-256 to uppercase hex, which is the shape this application already stores its
own token hashes in, and computed locally because a run id is not a token and the token service is not the
record's dependency. Dropping `CustomerId` outright was the alternative; it is kept because with D1 removing the
spend bound, the record of who ran what is worth **more**, not less.

**D6 — The access-token read stays.** Recorded with its evidence above, because the ask's premise was that
`[Authorize]` makes it unnecessary. It does not, and D6 is the one part of the request not carried out.

**D7 — A line's `amount` is carried, never multiplied.** The result schema, the prompt and the agent's own
contract all state that `amount` is *"this line's total for its quantity"*. The removed validator multiplied a
**unit** price by the quantity because it read the catalogue itself; the agent's value is already multiplied, so
`SuggestionCandidateLine.LineTotal` is now that value as it arrived. Multiplying it a second time would inflate
every candidate's price by its quantity, silently, on the page a customer reads.

## Consequences

**Good**

- Three fewer collaborators on the run: the controller depends on the agent, the settings, the request builder
  and the logger, and the graph behind a run is markedly smaller (`RunGuard`, `RunLease`, `SuggestionValidator`,
  `SuggestionSpread` all gone).
- The application no longer re-reads the catalogue mid-run for every line, so a run does less work between the
  agent's answer and the frame the customer sees.
- The free function of the old validator — adding the lines up — is still the application's, so a candidate's
  total cannot disagree with its own parts.

**Bad / recorded**

- **No spend bound per customer.** One caller can now start unlimited concurrent paid runs. The evaluation
  tier's stated plan was to set a cap from the run record's measurements; there is now no enforcement point at
  all, so that work would have to reintroduce one.
- **The application no longer proves a price.** The page can show a candidate the catalogue will not honour, and
  the customer finds out on **apply**. `SuggestionCandidate`, `SuggestionCandidateLine`, `SuggestionResultFrame`
  and `AiRunVerdict` all carried remarks asserting the opposite ("a candidate cannot carry a price the catalogue
  does not charge", "nothing is rendered from an unchecked answer"); every one of them was rewritten in this
  change, because a false guarantee left in a comment is worse than none.
- **Options are no longer range-checked**, so a run can present several candidates at effectively the same
  price.
- **A rejection mode is gone from the run.** `AiRunVerdict.Invalid` now means only "claimed a suggestion,
  carried none" — it can no longer mean "the catalogue disagreed".
- **`builder.run_in_flight` is removed from the API's published error codes.** A caller that branched on it has
  nothing to branch on, because the status it described cannot be returned.
- **Nothing here is verified at the browser tier**, which is red at HEAD for the reason
  [0006](0006-the-run-endpoint-is-a-controller.md) D6 gives. The move of failure from the run to the apply path
  is asserted nowhere; it rests on `AssignProductHandler` and the composition write, which are covered at their
  own tiers.

## Alternatives rejected

| Alternative | Why rejected |
|---|---|
| Keep `RunGuard` as a spend bound | The product owner's call: it is not wanted now. Recorded as the cost, above, rather than argued |
| Keep a narrower validator that only checks SKU, quantity and slot, taking the amounts from the agent | The middle path, and it was not asked for: it would keep the run failing for something the apply path already refuses |
| Drop `CustomerId` from `AiRunRecord` instead of hashing locally | Loses the record's only attribution, and the record's own contract documents the opaque id as deliberate |
| Keep `IOpaqueTokenService` in the controller for the record's hash | Keeps an unrelated dependency on the run for one line, and widens what the run needs |
| Write the account id into the record unhashed | The record is a log line whose contract says *"No PII"*; that would have to be rewritten to say the opposite |
| Remove the access-token read as well | Breaks every run's catalogue search: the agent's token service throws and has no fallback (Context, above) |
| Keep the `Checking the suggestion` stage | It would tell a customer their answer was being checked when nothing checks it |
| Multiply `amount` by `quantity` as the old validator did | Double-counts the quantity and inflates every price (D7) |

## Baselines

Measured on 2026-09-25, in the same working tree as [0006](0006-the-run-endpoint-is-a-controller.md), which also
carries unrelated in-flight work.

| Baseline | Before this change | After |
|---|---|---|
| `BUILD` | `dotnet build CoreRentalNet.sln` — **0 warnings, 0 errors** | **0 warnings, 0 errors** |
| `NONBROWSER` | **713 passed, 0 failed** | **712 passed, 0 failed** (−1: the 409 test, which no longer describes anything) |
| `BROWSER` | **red at HEAD**: 118 failed, 1 passed, 1 skipped — `Not built: …CoreRentalNet.E2E.LocalAgent.dll` | **unchanged**, same cause |
| `AGENT` | not run; nothing under `agentfoundry/` is touched | **40 passed, 0 failed** |

## Notes

- **One test was deleted and none was added.** The deleted one proved the 409; nothing in the codebase proved
  the validator, the spread rule or the guard's release path, so removing them removes no coverage that
  existed. What proves the endpoint still works is the boundary suite in `CoreRentalNet.Host.Tests`: the route,
  the gate, the 200 stream, and the two 400 refusals.
- **The test suite got simpler for a reason worth recording.** `BuilderApiTestHandler` existed because the guard
  keyed a customer by `ClaimTypes.NameIdentifier`, so a principal the guard could not name was refused as
  unkeyable. With the guard gone the builder suite reuses the catalogue's `CatalogApiTestHandler`, and one file
  and its duplicate header constant went with it.
- **`appsettings.Local.json` still lists `AgentFoundry:SpreadFactor`.** It is gitignored and development-only, and
  a key nothing reads is inert; it was left alone rather than edited as a personal file.
- **The four checks moved rather than vanished, and that is the claim worth testing next.** A run that suggests
  an unknown SKU should be refused by the composition write, not rendered and then rejected. There is no test
  for that path today.
