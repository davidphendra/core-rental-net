# e04s01 — only an entitled account sees the AI section

**Date:** 2026-09-16
**Branch:** `e03-catalogue-contract`

## What was run

| Check | Before | After |
|---|---|---|
| BUILD `CoreRentalNet.sln` | 0 warnings, 0 errors | **0 warnings, 0 errors** |
| NONBROWSER | 570 passed, 0 failed | **572 passed, 0 failed** |
| BROWSER | 104 passed, 0 failed, 1 skipped | **unchanged** |
| `agentfoundry/AgentFoundry.sln` | 38 passed | **unchanged** |

## What changed

- **`ClaimRequirement`** replaces `CatalogReadRequirement`, and now **carries** the claim it checks and
  its `ClaimBehavior` — `Open` or `Closed` — when no identity provider is configured.
- **`ClaimAuthorizationHandler`** replaces `CatalogReadAuthorizationHandler`: **one** handler for every
  permission, taking the claim from the requirement rather than from ambient configuration.
- **`ClaimSettings`** replaces `CatalogReadClaim`, reading `Authorization:<section>:ClaimType|Value`
  for any section.
- **`AiBuilderReadPolicy`** and **`AiBuilderPowerPolicy`** — `read:aibuilder` and `poweruser:aibuilder`,
  both `Closed`.
- **`appsettings.json`** gains the two sections, naming permission strings and nothing secret.
- `IdentityRegistration` and `Auth0SecuritySchemeTransformer` use the generalised settings.

## Measured effect

| | |
|---|---|
| Policies declared | 1 → **4** |
| Handlers answering them | 1 → **1** |
| Catalogue behaviour | **unchanged**, and the tests that assert it carried across |
| Behaviour with no identity provider | catalogue **open**, AI section **closed** |
| `poweruser:aibuilder` alone | satisfies **nothing** |

## What the run corrected

1. **A comment broke the configuration.** The new sections were introduced with a `//` note in
   `appsettings.json`, which the configuration reader accepts and `System.Text.Json` does not — three
   architecture tests parse that file strictly and failed at once. The explanation belongs in this
   record, not in a file that has to be strict JSON.
2. **The handler tests had to be re-pointed, not rewritten.** The claim moved from the handler to the
   requirement, so `Handler(withProvider, withClaim)` became `Handler(withProvider)` and the claim moved
   into the context's requirement. The assertions are unchanged, which is what makes them evidence that
   the catalogue's rule did not move.

## Not done, deliberately

- **The page is not gated yet.** `AIB-01` … `AIB-04` are asserted at the policy layer, which is where
  the rule lives; the section they guard arrives with `e04s03`. **`AIB-24`** — "a deployment with no
  identity provider shows no AI section anywhere on the page" — is a browser test and cannot exist
  before the page does. It moves to `e04s03` with the section.
- **`README` does not yet say the section needs an identity provider.** That sentence is a consequence
  of `P2` and belongs with the section a reader would look for.
