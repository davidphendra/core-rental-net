# E9 — Optional authentication with Auth0

**Slice:** 4 · **Status:** ready · **ADR:** 0016, 0017, 0018, 0019, 0020, 0021, 0022

## Goal
Let a customer sign in — optionally — so that an order can belong to someone and they can find
their own orders again, without disturbing the guest funnel that already works.

## In scope
- **Auth0 over standard OIDC**, with `Auth0.AspNetCore.Authentication` (1.11.0, targets net10.0) as
  a thin wrapper. Nothing bespoke.
- **Optional and additive.** No `Auth0:Domain` configured means OIDC is never registered, the
  sign-in affordance is hidden, and the application runs exactly as it does now. The application
  must start without identity configuration.
- **Two redirect endpoints**: `GET /account/login?returnUrl=…` and `GET /account/logout`, because a
  circuit has no `HttpContext` and cannot challenge, set cookies or redirect. `returnUrl` is
  validated as local.
- **Sign-in and sign-out controls**: the header and the Review step, replacing nothing. Initials
  rather than the provider's `picture`, so no page load reaches a third party.
- **An account menu in the header**: the initials sit before the bag and open a menu offering
  Profile and Sign out. Profile is a page showing the account's email, name and role; the menu is
  absent when no provider is configured.
- **A customer snapshot on the order**: `CustomerId` (the OIDC `sub`), `CustomerEmail`,
  `CustomerName`, all nullable, written once at checkout. A guest order leaves them null.
- **My rentals**: a list of the signed-in customer's own orders, plus the existing detail page
  re-authorised so that **ownership or the token** may open it.
- **Session policy**: 14-day sliding cookie, `HttpOnly`, `SameSite=Lax`, `Secure` in production; no
  access or refresh token stored; sign-out also redirects to the provider's logout.

## Out of scope
Organizations, a local customer entity, cancellation or extension from the UI, any revalidating
authentication state provider (ADR-0019 records the accepted consequence), any administrator
surface, multi-currency or i18n. **Superseded:** this slice originally left "roles as an
authorization mechanism" out of scope, with the role claim displayed only. A first amendment
gated the builder on the `Manager` role (ADR-0021); that is itself superseded — with the tenant's
API audience configured, the gate now checks the API permission **`read:catalog`** on the access
token, and the role is read from the same token (ADR-0022).

## Stories
1. As a guest I want the whole funnel to keep working, so that signing in is never required to
   evaluate the product.
2. As a returning customer I want to sign in and find my orders, so I do not depend on keeping a
   link.
3. As a customer whose session ends I want to sign out completely, so a shared computer does not
   silently re-enter my account.
4. As the business I want an order to record who placed it, as at the moment it was placed.
5. As a security reviewer I want a login page that cannot be turned into an open redirect, and no
   client secret anywhere in the repository.
6. As the product owner I want the identity provider to be optional at startup, so a deployment
   without it is a working deployment.

## Definition of done
Build clean with warnings as errors; every new matrix row below passing; the guest suite unaffected;
capsule archived with its evidence. The product owner decides acceptance.

## Matrix rows
AUTH-01 … AUTH-33

## Risks
- **The OIDC handshake is the integration most likely to be misconfigured** and it is the part no
  hermetic test can fully cover. Mitigated by a real handshake against a local provider, plus an
  opt-in test against the real tenant.
- **Test infrastructure we write ourselves can be wrong in the same way the application is wrong.**
  Mitigated by keeping the provider minimal and asserting observable behaviour.
- **A stale tab keeps acting signed in** (ADR-0019). Accepted, documented, not hidden.
- The application currently has no static-SSR page, which is why ADR-0017 chose endpoints over
  restructuring the render modes.
