# e05s06 — One entitled account, one run at a time

**type:** feat
**risk:** P0
**context:** app
**bcps:** 5
**status:** passing

## Context

A run is paid, abusable and customer-visible. The application is the entitlement authority: the agent
trusts nothing from the browser, and is never called until the application has decided the caller may
run and that no run is already in flight.

## Requirements

#### ADDED: a parameterised claim requirement

One claim requirement carrying claim type, claim value and **unconfigured behaviour**, with one handler.
The catalogue registers it `Open` — unchanged when no identity provider is configured — and the AI
permission registers it **`Closed`**: a deployment that has not been told which claim grants the feature
shows **no AI section** rather than an open one.

#### ADDED: the AI permission

A permission declared in configuration — section **`AIUse`**, i.e. `Authorization:AIUse:ClaimType` and
`Authorization:AIUse:ClaimValue` — closed when unconfigured. The claim's **value** is the identity
provider's to define and ours only to configure; the code never hard-codes one. A signed-in account
without it sees no AI section; the navigation and the endpoint agree on the same rule.

#### ADDED: the run guard, and no cap

One in-flight run per principal, released however the run ends — including on cancel. **No numeric
per-customer cap is set now.** The run record carries tokens, model and model-call count so the
evaluation tier sets the cap from measurement; a placeholder would look tuned without being so.

## Zoom-Out

- **Module purpose:** entitlement and concurrency control. It knows who may run and whether one is
  already running, and nothing else.
- **Callers:** the AI section and the suggestion endpoint.
- **Contracts to preserve:** the catalogue's own unconfigured behaviour must not change; the guard must
  be released on every exit path, or a cancelled run wedges a customer out.

## Steps

1. Add the claim requirement and handler; register the catalogue `Open` and re-run its tests unchanged.
   → verify: `dotnet test tests/CoreRentalNet.Host.Tests --nologo --filter "FullyQualifiedName~CatalogApiAuthorizationTests"`
2. Declare the AI permission `Closed`, reading its claim from configuration with an empty default.
   → verify: `dotnet build CoreRentalNet.sln -v q --nologo`
3. Add the one-in-flight-run guard, released on every exit path; no numeric cap.
   → verify: `dotnet test tests/CoreRentalNet.Host.Tests --nologo --filter "FullyQualifiedName~AiRunGuardTests"`
4. Tests AIWB-20 to AIWB-24, including the catalogue regression and the unconfigured case.
   → verify: `dotnet test tests/CoreRentalNet.Host.Tests --nologo`

## Outcome

Built and verified offline: `UnconfiguredBehaviour`, `ClaimRequirement` carrying it,
`ClaimAuthorizationHandler` applying it, `AiPolicy`, `ClaimSettings.AiUse`, `AuthorizationRegistration.AddAiAuthorization`,
`AiBuilder/RunGuard` and `AiBuilder/RunLease`; `AiAuthorizationPolicyTests` and `RunGuardTests`.

The catalogue's behaviour is unchanged and its assertions are untouched. Two construction sites
(`ClaimAuthorizationHandlerTests`, `CatalogApiAuthorizedFactory`) now pass `UnconfiguredBehaviour.Open`
explicitly, because the parameter is **required rather than defaulted** — the compiler asks every permission
the question, which is the whole point of the change.

## Security addendum

The story asked for this, so it is a real adversarial pass over the change rather than a paragraph saying it
was reviewed.

**Found and fixed — the zero value was the permissive one.** `UnconfiguredBehaviour` was declared
`Open, Closed`, so `default(UnconfiguredBehaviour)` — and any future zero-initialisation or deserialisation —
would have meant *allow*, in the very enum written to prevent that accident. **`Closed` is now declared
first**, so even the accident falls the safe way.

**Found and fixed — a second handler registration quietly denied everything.** `TryAddSingleton` looked like
the tidy way to let each permission register the shared handler. It is keyed on `IAuthorizationHandler`,
which the identity packages also register implementations of, so the application's handler was **silently not
added** and the catalogue's own tests caught it by answering 403. Reverted to `AddSingleton`: a duplicate
instance applies the same rule and is harmless, a missing one denies everyone. This is recorded in the
registration's own remarks, because it is the kind of tidy-looking change somebody will try again.

**Found and closed — a gate that only covered one permission.** `AUTH-15` asserts no claim value is written
into the application, but it greps for the catalogue's literal `read:catalog`, so a permission added later
was not covered by it. `AuthorizationGateTests` now also asserts the *shape* — nothing in the Host assigns a
claim value — and that the money-spending permission is declared `Closed` at its registration, so changing
that answer has to break a test to happen.

**Examined and found sound.** The comparison stays exact and case-sensitive (a composed wider value is
refused, asserted). An empty `AIUse` section in `appsettings.json` is unconfigured, not a wildcard. The
run guard's key is `NameIdentifier` then `Name`, and an account with neither is **refused** rather than keyed
on a shared empty string. `RunLease.Dispose` exchanges the delegate, so a double release cannot free a slot a
later run is holding.

**Recorded limitations, not defects.** The guard is in-process, so two application instances would each
allow one run per customer — the same shape as the rest of this application's in-memory state, and a
deployment that scales out needs a shared store. Authorising with the cookie is not required by the AI
policy, so a machine caller presenting the configured claim could also reach the endpoint; that is the same
permission, so it is noted rather than treated as a hole. No numeric per-customer cap exists, by design: the
run record is what will set it from measurement.

**The gap this story leaves open, deliberately, is the one that matters most.** Hiding a section is not
authorisation. Nothing here protects the endpoint that spends the money, because that endpoint does not exist
until `e05s07` — which, as the record stood, mentioned **no policy at all**. A requirement to gate it with
`AiPolicy.Name` has been added there, and the architecture test that enforces it on the catalogue controller
has been noted as the shape to copy.

## Security note

This story edits the claim requirement that the security review covers, in an epic that otherwise has
almost nothing to do with authentication. It needs a review addendum.
