# Core Rental

Rent a fully equipped office setup — desk, chair, monitors, plants, coffee — month to month, in
Bali. Compose it on a canvas, see what it costs as you go, and rent it.

This is a **demonstration**. No money changes hands, there is no payment provider in the code, and
you confirm a rental by typing `this is a demo`.

## What it does

1. **Design** a workspace on a fixed canvas: seven slots, one desk, one chair, up to three
   monitors, a lamp, a plant, a coffee station and a relax zone.
2. **Rent** it. You give a delivery address and type the phrase; the first month and a one-time
   delivery charge are recorded as an invoice and settled immediately.
3. **Work.** A background run schedules the delivery, activates the rental, and raises each monthly
   renewal exactly once until you cancel — which takes effect at the end of the month you have
   already paid for.

## Requirements

- **.NET SDK 10.0.400** (pinned in `global.json`)
- **Chromium for the browser suite.** Install it once with
  `pwsh tests/CoreRentalNet.E2E/bin/Debug/net10.0/playwright.ps1 install chromium`.
  Everything else runs without it. `pwsh` ships with GitHub runners; on macOS and Linux you can
  also use `npx playwright install chromium`.

## Run it

```bash
cd src/Host/CoreRentalNet.Host
dotnet run
```

Open the address it prints. The database is created on first run at
`src/Host/CoreRentalNet.Host/App_Data/corerental.db`.

Two things worth knowing:

- **The demo phrase is `this is a demo`**, typed into the checkout dialog. Capitalisation and
  surrounding spaces do not matter.
- **Start the application from its own directory**, as `dotnet run` does. A development build
  serves its stylesheets, fonts and product images from the project directory, so starting the
  built assembly from somewhere else gives you a working page with no styling.

### Signing in — optional

The funnel works entirely without an account. Signing in is additive: it puts your name and email
on the order and gives you a list of your own orders.

**With nothing configured, identity is simply off** — no sign-in link, no account routes, and the
application behaves exactly as described above. To turn it on against your own Auth0 tenant:

1. In the Auth0 dashboard, create an application of type **Regular Web Application**.
2. Register these URLs (the port is pinned, so they stay valid). Both are required.
   - **Allowed Callback URLs:** `http://localhost:5199/account/callback`, and
     `http://localhost:5199/swagger/oauth2-redirect.html` if you want to try the catalogue endpoint
     from the documentation page — the provider matches a callback exactly, so a missing entry fails
     the flow after the redirect with nothing said on the page.
   - **Allowed Logout URLs:** `http://localhost:5199/` — the application root, because sign-out
     hands the provider that address to return to rather than a route of ours.
     This one is at **Account Settings → Advanced → Allowed Logout URLs**, which is a *tenant*
     setting, not the application's own settings page. The provider says so itself when it is
     missing.
3. Put your **Domain**, **Client ID** and **Client secret** in
   `src/Host/CoreRentalNet.Host/appsettings.Local.json`, which is **gitignored** and loaded last:

   ```json
   {
     "Auth0": {
       "Domain": "…",
       "ClientId": "…",
       "ClientSecret": "…",
       "Audience": "https://your-api-identifier",
       "Scope": "openid profile email read:catalog",
       "LeewaySeconds": 180
     }
   }
   ```

   `Audience` is the API whose access token carries the account's permissions, and `Scope` names the
   permission the login asks for — a per-app authorization policy issues a permission only when the
   login requested it. Neither is set in `appsettings.json`, so a deployment with no API signs in
   exactly as it did before.

   `LeewaySeconds` is how far ahead of expiry the identity SDK refreshes the access token. Its shipped
   value of **180** (three minutes) is declared in `appsettings.json`, beside the audience and scope;
   naming it here overrides it. It matters because a suggestion run streams, and the agent presents the
   caller's token to the catalogue on every call for as long as the run lasts — so the margin has to
   exceed the longest run, or a run can outlive its own token. The identity SDK's own default is 60
   seconds, and the API's **Maximum Access Token Lifetime** must be longer than whatever is set here.

   The same three values drive the development documentation page at `/swagger`, which authorizes
   with the same client and asks for the same scopes. One thing is worth knowing about PKCE: the
   provider documents it for clients that cannot hold a secret — single-page and native applications —
   while a regular web application is a confidential client, so a tenant that refuses the exchange
   without a secret for it needs a public client for that page alone:

   ```json
   {
     "Swagger": { "ClientId": "your-public-client-id" }
   }
   ```

   Naming one changes nothing about the sign-in, and the page uses `Auth0:ClientId` when it is unset.

   `appsettings.Development.json` stays as the tracked, empty template, and **the local file is
   read in development only** — a deployed environment ignores it entirely, so a stray copy cannot
   override production settings. A test holds that guard in place, and another fails the build if a
   secret appears in a file that is committed.

   Precedence in development, lowest to highest: `appsettings.json` → `appsettings.Development.json`
   → user secrets → environment variables → **`appsettings.Local.json`**. The local file is loaded
   last, so on a developer's machine it is the final word — which is the point of it.

4. **The role is read from the access token.** The tenant names one permission per role —
   `manager:role`, `supervisor:role`, `staff:role`, `guest:role` — so the role is the permission's
   name before `:role`, shown with its first letter capitalised. Assign a role under **User
   Management → Users**, then open that user's **Roles** tab, and the profile's Role line follows.

   A tenant that would rather put roles in the token itself has to do it with an Action, because
   Auth0 issues roles in no token on its own: reach for `api.accessToken.setCustomClaim` (or
   `api.idToken.setCustomClaim`) and attach the Action to the **Login** flow. The application still
   reads the claim named by `Auth0:RoleClaimType` (default
   `https://core-rental.periang.auth0/roles`) and any claim whose own name ends in `/roles`, in
   addition to the role permission. With none of that, the profile's Role line says the provider
   sent none — the honest answer rather than a label the application invented.

5. **The builder is gated by the `read:catalog` permission.** Auth0 writes an account's permissions
   into the **access token** — never the ID token — and only when the login names an API's
   identifier as the `audience`. To set it up:

   - Create an **API** with an identifier (for example `https://corerental/api`).
   - Add the permission **`read:catalog`** on its **Permissions** tab.
   - Under **Settings → RBAC Settings**, enable **Enable RBAC** and **Add Permissions in the Access
     Token**.
   - Assign the permission to a role the account holds, under the role's **Permissions** tab. A
     `Per-app authorization` policy also needs the application granted, on the API's **Application
     Access** tab.
   - Set `Auth0:Audience` to the identifier and add `read:catalog` to `Auth0:Scope`, beside the
     credentials in `appsettings.Local.json`.

   The gate itself is `Authorization:CatalogRead:ClaimType` = `permissions` and `:ClaimValue` =
   `read:catalog`. An account that carries it may open the builder; a signed-in account that does
   not is told the builder is refused. The header's link is hidden unless the account carries the
   claim, so the affordance and the gate agree.

6. **Allow Offline Access, or a session ends at the first token expiry.** The access token Auth0 issues is
   short-lived; the sign-in cookie is not. The application asks the identity SDK for a refresh token and the
   SDK exchanges it whenever the access token has expired - which is what keeps a session usable past the first
   expiry, and the SDK adds `offline_access` to the login's scopes on its own. Auth0 issues a refresh token only
   when the API has **Allow Offline Access** switched on (**APIs → your API → Settings**). Without it the SDK
   finds no refresh token, ends the local session and sends the customer back to sign in; a session that was
   already signed in when the setting was turned on is asked to sign in once. **Refresh Token Rotation** on the
   same page is worth enabling, and the SDK stores the rotated token it returns. How far ahead of expiry the
   SDK refreshes is `Auth0:LeewaySeconds` — **180**, three minutes, by default — because a suggestion run
   streams and the agent keeps presenting the caller's token to the catalogue while it does; the API's
   **Maximum Access Token Lifetime** must be longer than that margin.

7. Run the app and use the sign-in link in the header.

**Signing out takes one extra click.** The application clears its own cookie and sends the browser
to the provider's logout endpoint; without an `id_token_hint` the provider asks for confirmation
before ending its own session. That is the provider's screen, not ours.

A test fails the build if a client secret ever appears in a committed configuration file.

For any other environment, set `Auth0__Domain`, `Auth0__ClientId` and `Auth0__ClientSecret` as
environment variables or application settings, and register that environment's own callback and
logout URLs in the tenant. Sign-in requires **HTTPS** outside development, because the
authentication cookie cannot be marked `Secure` over plain HTTP.

### Start over

The catalog is read once at startup and the database is disposable:

```bash
rm -rf src/Host/CoreRentalNet.Host/App_Data
```

Editing `src/shared/data/products.json` has no effect until you do that.

## Test it

```bash
dotnet test CoreRentalNet.sln --filter "FullyQualifiedName!~CoreRentalNet.E2E"   # 529 tests
dotnet test tests/CoreRentalNet.E2E                                              # 103 in a browser
```

| Layer | What it proves |
|---|---|
| Unit | The domain: money, slots, the workspace, orders, invoices, the schedule |
| Integration | Against a real SQLite file, a real catalog file and a real database schema |
| Architecture | Module boundaries, table prefixes, no clock reads, no payment code |
| Browser | The whole funnel in Chromium, with **no mocks and no intercepted calls** |

The browser suite starts a real application process with its own throwaway database, and one test
asserts that nothing sits between the browser and the server. A second process is the identity
provider the sign-in tests drive a real OIDC handshake against, on localhost, from this repository
(`tests/CoreRentalNet.E2E.LocalProvider`) - so authentication is exercised without a tenant, secrets
or the public internet. It exists because four separate bugs in this project were invisible to a
green unit suite and to every command-line check.

One test is opt-in and reaches a **real tenant**. It is skipped unless credentials are in the
environment, because it needs a tenant, a real account and the network - none of which belongs in
the hermetic suite:

```bash
CORERENTAL_TENANT_EMAIL=you@example.com CORERENTAL_TENANT_PASSWORD=... \
  dotnet test tests/CoreRentalNet.E2E --filter FullyQualifiedName~RealTenantTests
```

Add `CORERENTAL_TENANT_AUDIENCE` when the tenant has an API whose access token carries the gate's
permission; without it the test asserts the refusal the missing audience produces. The tenant's
credentials themselves come from `appsettings.Local.json`, and `CORERENTAL_TENANT_URL` reuses an
already-running host instead of starting one on `http://localhost:5199`.

## Where things are

```
src/BuildingBlocks/     Money, opaque tokens, the business calendar, SQLite plumbing
src/Modules/Catalog/    205 products, read-only in memory, no database at all
src/Modules/Discovery/  The similarity search, the name search, the vector file's freshness; owns no tables
src/Tools/              CoreRentalNet.CatalogIngestion, the deliberate step that builds that index
src/Modules/Workspace/  The draft: slots, quantities, delivery address, the quote
src/Modules/Rentals/    Orders, invoices, periods, the schedule
src/Host/               The Blazor application, its components and its design tokens
src/shared/data/        products.json, the single source of truth for every price
specs/                  the baselines, the decisions, and the design prototypes
```

`specs/` is the record of why, not just what:

- `specs/baselines.md` — what `BUILD`, `NONBROWSER`, `BROWSER` and `AGENT` mean, and where each is
  measured
- `specs/adr/` — the decisions, with the alternatives that were rejected and why
- `specs/design/` — design prototypes, kept as references

The plan documents this file once listed — a release plan, epic capsules and a commit plan — were
removed deliberately and are not coming back. `CONVENTIONS.md`, the ADRs and this file carry the rest.

## Decisions worth knowing before you read the code

- **Money** is `decimal` in IDR, rounded once per line, and the client never sends an amount that
  influences a charge. The draft stores product references; prices are recomputed from the catalog
  on every read and frozen only when an invoice is issued.
- **Two local SQLite files**, one write process each, module isolation by table prefix. The
  application database (`Sqlite:DatabasePath`) and the catalogue vector file (`Database:Path`, built
  deliberately by `CoreRentalNet.CatalogIngestion`) are separate because the vectors are derived data
  seeded per environment rather than schema. The ingestion tool is standalone — it references no
  discovery project and writes only its own file. WAL locally; a rollback journal is required on a
  network filesystem, which is why the journal mode is configuration. This application must never
  scale out.
- **No payment provider.** Payment cannot fail by construction, so there is no retry, no dunning
  and no failure state to model.
- **No accounts.** A draft is addressed by an opaque token in an `HttpOnly` cookie; an order by a
  token in its confirmation link. Numbers are labels, never credentials.
- **No administrator.** The only thing that moves an order forward is the schedule.

## Known limitations

- **The confirmation link is the only way to see an order.** A repeated checkout cannot reproduce
  the access token, so it lands on a page that says so and tells the customer to keep their link.
  With no account and no email, that is inherent rather than an oversight.
- **The scheduler reads invoices one order at a time.** Fine for a demonstration; it would need a
  projection before it met a large book.
- **No deployment configuration.** There is a CI workflow, but nothing deploys. Any deployment is a
  deliberate decision that has not been made yet, and it would need the database path and journal
  mode pointed at persistent storage.

## Versioning

The release identity is the Git tag, and the pipeline is where it is resolved. The build never reads
Git: `scripts/resolve-version.sh` turns the tag being built into a semantic version plus the short
commit, and the pipelines pass that to `dotnet build` and `dotnet publish`. `Directory.Build.props`
turns the one value into the three .NET properties:

| Property | Value | Example |
|---|---|---|
| `AssemblyVersion` | stabilized `MAJOR.MINOR.0.0` | `1.4.0.0` |
| `FileVersion` | the release `MAJOR.MINOR.PATCH.0` | `1.4.1.0` |
| `InformationalVersion` | the release plus the commit as build metadata | `1.4.1+abc1234` |

The footer shows that version, read from the artifact's own assembly information; the commit is trimmed
away, so the footer is a version rather than a fingerprint. A build with no tag keeps the floor `0.1.0`.

The agent tree is versioned the same way, with one difference: Foundry builds the deployed agent
remotely from a zipped project folder, so the repository's props files are not there. The pipeline
writes `Version.g.props` inside `agentfoundry/src/CoreRentalNet.Agents/` instead, and that project
carries its own copy of the mapping and prints the version in its startup diagnostics.

The reason for each choice, and the alternatives that were rejected, is in `specs/adr/`.

## Continuous integration

`.github/workflows/ci.yml` builds, runs the non-browser tests, installs Chromium and runs the browser
tests. It has **never executed**: this repository has no remote yet. The file is committed so that the
first push is the first run.

It builds and tests `CoreRentalNet.sln` only, so the agent tree's suite does not run in it — a
path-filtered job for `agentfoundry/AgentFoundry.sln` is `e05s09`'s, and without it the two-tree
separation is nominal. As measured on 2026-10-05: **827** non-browser tests and **183** agent tests,
both 0 failed. The browser tier could not be measured: its `CoreRentalNet.E2E.LocalAgent` stand-in was
deleted in `a6e127e` while `HostFixture` still starts it, so the suite cannot start. The figure this
paragraph carried before — 636, 104 and 63 — had been stale for long enough that nobody could say
when it stopped being true. The four baselines and their latest numbers are in `specs/baselines.md`.
