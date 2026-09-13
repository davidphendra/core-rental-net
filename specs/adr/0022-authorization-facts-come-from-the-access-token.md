# ADR-0022: Authorization facts come from the access token

- **Status:** Accepted (2026-09-13)
- **Supersedes:** ADR-0021 (the gate is a role). **Amends ADR-0019** on storing the access token.

## Context
The builder's gate shipped as a **role** (ADR-0021) because a probe found no API in the tenant and
roles were the only entitlement Auth0 would issue. That reading was incomplete. The tenant does have
an API — identifier `https://0z1ZZMu6afVqQc1iPI9H.periang.local` — and once the login named it as
the `audience`, Auth0 issued a **JWT access token** carrying the account's RBAC permissions.

Verified end to end on 2026-09-13 against the real tenant, account `budi.manager@mailinator.com`:

```json
{ "aud": ["https://0z1ZZMu6afVqQc1iPI9H.periang.local",
          "https://dev-ov6cn5bhs4xc00tr.us.auth0.com/userinfo"],
  "scope": "openid profile email read:catalog",
  "permissions": ["manager:role", "read:catalog"] }
```

Three facts changed the design:

1. **The permission is `read:catalog`, not `catalog:read`.** The earlier assumption (and an ADR-0021
   probe) used the wrong value; the app read the claim correctly and the gate simply expected a
   permission the tenant never issued.
2. **Auth0 only writes permissions to an access token**, and only when the login names the API's
   audience — never to an ID token, and a post-login Action cannot see them.
3. **A role is a bundle of permissions, and the tenant names one permission per role**
   (`manager:role`, `supervisor:role`, `staff:role`, `guest:role`). The role can therefore be read
   from the access token instead of a claim an Action must be written to add to the ID token.

## Decision
- **The access token is the source of authorization facts.** The login requests the API audience
  (`Auth0:Audience`) and the permission as a scope (`Auth0:Scope`); the token's `permissions` claim
  is lifted into the identity at sign-in.
- **The gate is a permission.** `Authorization:CatalogRead:ClaimType` / `:ClaimValue` ship as
  `permissions` / `read:catalog`. It is still configuration, so a deployment can point it elsewhere.
- **The role is read from the access token too.** A permission whose name ends in `:role` states the
  role of the same name (`RoleClaims`), shown with its first letter capitalised. The ID-token claim
  the Action emits is still read as a fallback when no role permission is present.
- **The session keeps the access token.** The application uses the SDK's `WithAccessToken` and
  `SaveTokens = true`, and a `TokenHandler` is registered to put the token on an outbound call. This
  amends ADR-0019: the token is no longer discarded at sign-in, because a per-app authorization
  policy issues a permission only when the login asked for it and that token is where it arrives.
- **The builder's nav link follows the entitlement** — hidden unless the account carries the claim —
  while the page keeps its existing behaviour (guest → sign in; signed-in without it → refused).

## Consequences
- **ADR-0021 is superseded.** Roles no longer decide anything; the permission does. The role is
  display only, as it was before ADR-0021.
- **`read:catalog` is now written into the committed configuration**, so the source-level check
  (AUTH-15) that no claim value is hard-coded in the application still holds: the value lives in
  `appsettings.json`, not in code.
- **The tenant's permission name is part of the deployment contract.** Renaming it in Auth0 requires
  the matching configuration change. This is the cost of gating on the provider's own rule.
- **A token is now at rest in the session cookie.** ADR-0019 accepted "fewer secrets at rest" when
  there was no API to call; that trade is given up deliberately, and the counterweight is that the
  token is never returned to the browser and the cookie is `HttpOnly`.

## Alternatives rejected
- **Keep gating on the `Manager` role (ADR-0021).** Coarser, and it makes roles decide access, which
  the E9 slice had explicitly out of scope. The tenant's permission is the finer answer.
- **Invent `catalog:read` in the tenant to match the assumption.** Adding a permission nobody meant
  so that the code's guess would be right is backwards; the code follows the tenant.
- **Write roles into the ID token with an Action and read them there.** Auth0 states roles must be
  added by an Action on whichever token carries them; deriving the role from the permission the
  tenant already issues is one fewer tenant rule to keep in step.
