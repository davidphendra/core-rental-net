# ADR-0020: Testing authentication

- **Status:** Accepted (2026-09-10)

## Context
The browser suite may not mock or intercept anything, and it asserts that: no route handlers, no
service worker, and a direct fetch from the page that must reach the server. A real identity
provider needs a tenant, a real user, credentials and the public internet, all of which would sit
inside a suite whose value is being hermetic and offline-runnable. But the OIDC handshake — callback
URL, scopes, claim mapping — is precisely where configuration mistakes live.

## Decision
- **A minimal OpenID provider lives in the repository**, used only by the browser suite. The
  application points at it through the same `Authority` setting it would point at a real tenant, so
  the suite drives a **real OIDC handshake** against a real server on localhost. Nothing is
  intercepted and the browser talks only to servers this repository starts.
- **The provider is refused in Production**, by a guard that fails the boot rather than a
  convention, so it cannot be enabled by a stray setting.
- **An opt-in test validates the real tenant** when credentials are present. Network-required,
  skipped by default, excluded from CI. It is the only thing that can verify the tenant-specific
  configuration, and it is named so nobody mistakes it for part of the normal suite.
- The provider's scope is deliberately tiny, and the suite asserts the application's **observable**
  behaviour rather than protocol details.

## Consequences
The honest limitation: **test infrastructure written here can be wrong in the same way the
application is wrong.** The mitigation is that the provider stays minimal, the assertions are about
what the customer sees, and the real tenant keeps an opt-in test of its own.

## Alternatives rejected
A battle-tested provider package (safer, but the one dependency this project has consistently
avoided), a development-only first-party cookie scheme (cheap, but leaves the OIDC wiring itself
untested), and a real tenant inside CI (secrets and the public internet inside the suite that exists
to be independent of both).
