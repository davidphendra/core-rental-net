# ADR-0021: The builder's gate is a role

- **Status:** Superseded by ADR-0022 (2026-09-13)

## Context
E9 gated the builder by a claim that is configured rather than hard-coded, and shipped it pointing
at an API permission (`read:catalog` on the access token). That is the model Auth0 documents for
authorization, but it requires the tenant to have an **API audience**: Auth0 writes `permissions`
only to a token it issues for an API. The tenant this project signs in against has none — a probe
of `/authorize` with `https://corerental/api` answers `Service not found` — and its client is not
granted the Management API. So no real sign-in could ever produce the claim the gate wanted.

What the tenant does issue is **roles**, through a post-login Action that adds a namespaced
`/roles` claim to the ID token. Verified end to end on 2026-09-12: the account supplied for the
check (`budi.manager@mailinator.com`) signs in and carries the role `Manager`, and the
permission-gated builder correctly but uselessly refused it.

## Decision
The builder's gate stays configuration — `Authorization:CatalogRead:ClaimType` and
`:ClaimValue` — and that configuration ships pointing at the **role** the tenant already delivers,
`Manager` under `https://core-rental.periang.auth0/roles`. An account that carries it may open the
builder; a signed-in account that does not is told the builder is refused, not sent to sign in
again. A deployment that has an API audience points the same two settings at the permission claim
instead; no code changes either way.

## Consequences
- **The entitlement is coarser than a permission.** Any account the tenant gives the `Manager`
  role can open the builder, wherever that role is assigned. A permission can be scoped to one API
  and one action; a role is a bundle. For a demonstration whose tenant is set up this way, that is
  the honest trade rather than a hidden one.
- **Roles now decide something.** The E9 slice had this explicitly out of scope ("the role claim
  is displayed and decides nothing"); that note is superseded here.
- The permission path is not removed. `PermissionClaims` still lifts an access token's
  `permissions` into the identity when `Auth0:Audience` is configured, and the gate can be pointed
  at it, but nothing in the shipped configuration uses it.
- The claim type has to match the tenant's Action namespace exactly; the README's Action example
  uses the namespace the defaults expect so that copying it works.

## Alternatives rejected
- **Create the API and permission in the tenant.** The documented path, and the right one when
  permissions are the model, but it makes every deployment of a demonstration depend on a piece of
  tenant setup that the tenant's own roles already make unnecessary.
- **Hard-code the role.** Removes the configurable gate that d777a22 deliberately introduced, and
  would make a permission-based deployment a code change. The mechanism is right; only the value
  changed.
- **Read permissions from an ID token.** Auth0 does not put them there, and an Action cannot see
  `event.authorization.permissions`; writing them into the ID token by hand would be this
  application inventing an authorization answer rather than reading the provider's.
