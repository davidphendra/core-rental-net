# ADR-0019: Session policy

- **Status:** Accepted (2026-09-10)

## Decision
- **Cookie:** sliding expiration of 14 days (the framework default), `HttpOnly`, `SameSite=Lax`,
  and `SecurePolicy = SameAsRequest` in development versus `Always` in production. Over plain HTTP
  the cookie cannot be marked `Secure`, so **local development is inherently weaker than
  production**; the deployment requirement is HTTPS.
- **No access token and no refresh token are stored.** Only `openid profile email` is requested,
  claims are taken from the ID token during the callback, and nothing else is kept. There is no API
  of ours to call and nothing at read time calls the provider, because the order carries its own
  snapshot (ADR-0018). Fewer secrets at rest means nothing to leak.
- **Sign-out signs out of the provider too**, by redirecting to its logout endpoint. Signing out
  locally only would leave the button not really signing anyone out on a shared computer, which is
  worse than the inconvenience.
- **No revalidating authentication state provider.** The circuit keeps the authentication context
  for the lifetime of the connection, which is the documented framework behaviour.

## Consequences, stated rather than buried
With no revalidation, **a tab left open past the cookie's expiry can still read that user's own
orders until the connection reconnects**. It is not a cross-user exposure: the principal is that
user's, every query is still filtered by it, and there is no mutation surface because cancellation
is out of scope. It is session expiry not being enforced in a stale tab, which is accepted here and
would be a small addition later.
