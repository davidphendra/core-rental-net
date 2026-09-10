# ADR-0007: Server-persisted draft behind an opaque cookie token

- **Status:** Accepted (2026-09-10)
- **Decided by:** product owner (server persistence), architecture (mechanism)

## Context
No accounts, but the funnel spans Builder → Store → Review → Rent. A draft must
outlive a page, and in-memory circuit state would die on refresh and could not be
exercised by a reloading browser test.

## Decision
- The draft is persisted in SQLite and addressed by a **128-bit opaque token** in
  an `HttpOnly; Secure; SameSite=Lax` cookie. The draft's own identifier is never
  exposed, so the id space cannot be walked.
- The token lifetime is owned by **HTTP middleware**, not components.

## Verified constraint behind this
Blazor Server runs component event handlers over the SignalR circuit, not in an
HTTP response, so a component **cannot set a cookie**. The token is issued by
middleware on the first GET and rotated on the forced full-page navigation to the
confirmation view after checkout — which is also how the cart reset happens.
`IHttpContextAccessor` is unreliable inside a circuit scope, so the token is
captured during prerender into a scoped provider.

## Consequences
- Reload, second tab and back-button all behave correctly; the E2E suite can
  assert "cookie token changed and the old draft is gone" as a literal test of
  cart reset.
- Optimistic concurrency per ADR-0003 guards two-tab edits.
