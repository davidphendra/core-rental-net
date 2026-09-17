# e04s01 — Only an entitled account sees the AI section

**type:** feat
**risk:** P0
**context:** host
**bcps:** 3
**status:** passing

## Context

Two permissions are needed — `read:aibuilder` to reach the section, `poweruser:aibuilder` to widen the
number of candidates — and the existing mechanism cannot express them. `CatalogReadRequirement`
**carries nothing** and its handler decides from configuration, including the rule *"with no provider
configured … the catalog is open"*. That rule is wrong for this feature: reading the catalogue is
served from a 62-row in-memory array, while one suggestion costs **up to ten model calls to an external
service**.

So the requirement must carry information, and once it does, the smaller design is the general one.

## Requirements

#### MODIFIED: how entitlement is decided

**Before:** `CatalogReadClaim` (typed, bound to `Authorization:CatalogRead:*`),
`CatalogReadRequirement` (carries nothing), `CatalogReadAuthorizationHandler` (opens when unconfigured;
exact, case-sensitive match).

**After:** one **`ClaimRequirement`** carrying the claim type, the claim value and an explicit
**`unconfigured: Open | Closed`**, answered by **one** handler; policies registered per permission with
their own configuration keys. The catalogue's rule is preserved exactly — `Open` — and the AI section
is `Closed`. Entitlement is decided once, as `CatalogApiPolicy` already states it should be.

#### ADDED: the AI section's two permissions

`read:aibuilder` gates the section; `poweruser:aibuilder` widens the number of candidates shown and
**implies nothing on its own** — holding it without `read:aibuilder` grants no access.

## Zoom-Out

- **Module purpose:** `Host/Infrastructure` and `Host/Composition` answer "who is entitled to what".
- **Callers:** `Builder.razor` (already gated by the catalogue policy), the header's affordance, and
  the AI section's own gate.
- **Contracts to preserve:** `Builder.razor`'s existing `[Authorize(Policy = CatalogPolicy.Name)]`;
  the catalogue's behaviour; the exact, case-sensitive comparison; the rule that a blank claim
  configuration denies rather than allows.

## Steps

1. Add `ClaimRequirement` carrying claim type, claim value and unconfigured behaviour, with **one**
   handler. → verify: `dotnet build CoreRentalNet.sln -v q --nologo`
2. Register the catalogue policies against `Open` and re-run the existing authorisation tests
   unchanged. → verify: `dotnet test tests/CoreRentalNet.Host.Tests --nologo --filter "FullyQualifiedName~CatalogApiAuthorizationTests"`
3. Register the AI policies against `Closed`, with their own configuration keys and a typed claim
   record. → verify: `dotnet build CoreRentalNet.sln -v q --nologo`
4. Declare and test the two AI policies and their entitlement rules. The **page** that consumes them
   arrives with `e04s03`; `AIB-24` moves there with it. → verify: `dotnet test tests/CoreRentalNet.Host.Tests --nologo --filter "FullyQualifiedName~ClaimAuthorizationHandlerTests"`
5. Unit tests AIB-01 … AIB-05, including the catalogue regression. → verify: `dotnet test tests/CoreRentalNet.Host.Tests --nologo --filter "FullyQualifiedName~ClaimRequirementTests"`
6. Add the security-review addendum naming this story. → verify: `grep -c "e04s01" specs/security/REVIEW.md`

## Verification Script (Step-by-Step)

1. Run the app with no identity provider → no AI section anywhere on `/builder`, and the builder works
   as before.
2. Run with the local OIDC provider and an account holding `read:catalog` only → no AI section.
3. Add `read:aibuilder` → the section appears; one candidate is offered after a run.
4. Add `poweruser:aibuilder` → three candidates are offered.
5. Hold `poweruser:aibuilder` alone → no section.
6. Confirm the catalogue pages behave exactly as before in every case.

## Test matrix

| ID | Scenario | Level |
|----|----------|-------|
| AIB-01 | Both permissions reach the section; the power grant widens the count | unit |
| AIB-02 | Without `read:aibuilder` there is no section | unit |
| AIB-03 | No provider configured means no section | unit |
| AIB-04 | `poweruser:aibuilder` alone grants nothing | unit |
| AIB-05 | The catalogue's open-when-unconfigured rule still holds | unit |

## Out of scope

- The section's content and the apply path (`e04s02` … `e04s05`).
- Any change to the catalogue's own entitlement or to sign-in.

## Risks

- **This edits security-reviewed code in a UI epic.** `specs/security/REVIEW.md` covers
  `CatalogReadAuthorizationHandler`; the refactor must preserve its behaviour exactly, which is what
  AIB-05 asserts, and it needs a review addendum.
- **One handler becoming a behaviour switchboard.** It carries three values and answers a boolean; the
  matching rule stays exact, case-sensitive, and denying on a blank configuration.
- **A hidden control mistaken for a control.** The candidate count is decided in the Host, never by
  hiding a candidate in the markup.

## Acceptance criteria

- AIB-01 … AIB-05 pass, with AIB-05 proving the catalogue's rule did not move.
- The entitlement rules hold at the policy layer, which is where the rule lives. "No AI section is
  rendered with no provider configured" is a page assertion and moves to `e04s03` as `AIB-24`.
- The security review records the addendum.
