# ADR-0016: Authentication is optional and additive

- **Status:** Accepted (2026-09-10), amended 2026-09-12
- **Amends:** ADR-0009, whose "no identity provider for the MVP" is now superseded in part

## Context
ADR-0009 deferred authentication on the grounds that there was no consumer for a customer's
identity: no notification email was sent, and there was no admin to read a contact. A signed-in
customer and a list of their own orders is exactly that missing consumer, so the reasoning behind
the earlier decision no longer holds. What has *not* changed is the funnel: four designed screens,
all of them usable without an account, and a demonstration whose value is being showable in thirty
seconds.

## Decision
- **Authentication is optional and additive.** Guests keep the entire funnel exactly as it is. The
  token-addressed confirmation link remains the guest's way back to an order.
- Signing in additionally: records the customer on the order, and unlocks a list of that customer's
  own orders.
- **The workspace draft stays per browser.** Signing in moves nothing, claims nothing and merges
  nothing. Only the *order* gains an owner. The `Workspace` aggregate is untouched.
- **The application starts without any identity configuration.** No `Auth0:Domain` means OIDC is not
  registered, the sign-in affordance is hidden, and the application behaves exactly as it did
  before this epic. A working application must not refuse to start because of a feature nobody is
  using.
- **Roles are displayed, never enforced.** The account's role claim is shown on the profile page
  and decides nothing: the only authorization rule is ownership. No organizations.

## Consequences
- The guest half of the browser suite is unaffected and keeps passing unchanged.
- An order is the only thing that acquires an owner, so no existing table is restructured.
- A guest order can never appear in "my rentals": there is nothing to match it on, since no email
  was ever captured. Its link remains its only address, and that is stated rather than implied.
