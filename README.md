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
2. Register these URLs (the port is pinned, so they stay valid):
   - Allowed Callback URLs: `http://localhost:5199/account/callback`
   - Allowed Logout URLs: `http://localhost:5199/`
3. Paste the **Domain** and **Client ID** into
   `src/Host/CoreRentalNet.Host/appsettings.Development.json` under `Auth0`.
4. Keep the **client secret** out of every file:

   ```bash
   dotnet user-secrets set "Auth0:ClientSecret" "<your-secret>" \
     --project src/Host/CoreRentalNet.Host
   ```

5. Run the app and use the sign-in link in the header.

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
dotnet test CoreRentalNet.sln --filter "FullyQualifiedName!~CoreRentalNet.E2E"   # 282 tests
dotnet test tests/CoreRentalNet.E2E                                              # 43 in a browser
```

| Layer | What it proves |
|---|---|
| Unit | The domain: money, slots, the workspace, orders, invoices, the schedule |
| Integration | Against a real SQLite file, a real catalog file and a real database schema |
| Architecture | Module boundaries, table prefixes, no clock reads, no payment code |
| Browser | The whole funnel in Chromium, with **no mocks and no intercepted calls** |

The browser suite starts a real application process with its own throwaway database, and one test
asserts that nothing sits between the browser and the server. It exists because four separate bugs
in this project were invisible to a green unit suite and to every command-line check.

## Where things are

```
src/BuildingBlocks/     Money, opaque tokens, the business calendar, SQLite plumbing
src/Modules/Catalog/    62 products, read-only in memory, no database at all
src/Modules/Workspace/  The draft: slots, quantities, delivery address, the quote
src/Modules/Rentals/    Orders, invoices, periods, the schedule
src/Host/               The Blazor application, its components and its design tokens
src/shared/data/        products.json, the single source of truth for every price
specs/                  ADRs, architecture, the test matrix, epic capsules, verifications
```

`specs/` is the record of why, not just what:

- `specs/adr/` — 15 decisions, each with the reasoning and the facts behind it
- `specs/architecture.md` — the module map, the trust model, the measured SQLite constraints
- `specs/test-matrix.md` — 101 scenarios and the layer that covers each one
- `specs/verifications/` — what was actually run and observed, including what each run corrected
- `specs/epics/archive/` — the eight epics with their evidence and their deliberate omissions

## Decisions worth knowing before you read the code

- **Money** is `decimal` in IDR, rounded once per line, and the client never sends an amount that
  influences a charge. The draft stores product references; prices are recomputed from the catalog
  on every read and frozen only when an invoice is issued.
- **One SQLite file**, one instance, module isolation by table prefix. WAL locally; a rollback
  journal is required on a network filesystem, which is why the journal mode is configuration.
  This application must never scale out.
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

## Continuous integration

`.github/workflows/ci.yml` builds, runs the 282 non-browser tests, installs Chromium and runs the
43 browser tests. It has **never executed**: this repository has no remote yet. The file is
committed so that the first push is the first run.
