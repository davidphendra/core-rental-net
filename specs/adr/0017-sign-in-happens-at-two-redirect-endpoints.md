# ADR-0017: Sign-in happens at two HTTP redirect endpoints

- **Status:** Accepted (2026-09-10)
- **Amends:** ADR-0012, whose "no public HTTP API" becomes "no REST API for clients"

## Context
Microsoft's Blazor security documentation is explicit that in an interactive circuit
*"authentication is managed via the SignalR hub and entirely within the circuit"*, and that
sign-out on GET *"is only valid for static SSR because `HttpContext` is null during interactive
rendering"*. This application is the opposite shape from Microsoft's templates: `App.razor` renders
statically so it can read the draft token, but `Routes` is `InteractiveServer`, so **every page is
interactive and there is no static-SSR page at all**. OIDC challenge and sign-out are HTTP verbs
that need an `HttpContext` and must write response cookies and redirects, none of which a circuit
can do.

## Decision
Two endpoints, and nothing else gains an HTTP surface:

- `GET /account/login?returnUrl=…` issues the OIDC challenge.
- `GET /account/logout` signs out locally and redirects to the authority's logout endpoint.

- The sign-in control is an ordinary anchor, so it is a real navigation. The circuit ends and a new
  one begins; the in-progress workspace survives because it lives in a cookie, not in the circuit.
- **`returnUrl` is validated as local** before redirecting, or the login page becomes an open
  redirect.
- The alternative considered and rejected was restructuring the whole application to per-page
  render modes: a large blast radius, and it would re-open how the draft token reaches every
  interactive page.

## Consequences
ADR-0012's decision is preserved in substance — no JSON contracts, no CORS, no bearer tokens, no
API for a client to consume — and corrected in letter, because the application does now have an
HTTP surface of two redirects. Saying so is cheaper than maintaining that it has none.
