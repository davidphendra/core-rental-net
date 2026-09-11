# E3 verification — running the application

> **Partially superseded.** The draft cookie rotated correctly, but a later browser run found that
> every circuit shared one workspace (a Razor attribute-literal mistake in the draft token), so any
> observation here that depended on per-browser drafts was measuring a shared draft. See
> `E5-browser-walkthrough.md` for the corrected picture. The routing, gating and rendering
> observations below still stand, with two exceptions, both on the store page: it no longer carries
> the selection panel, and it no longer lists extras on arrival. It opens on desks and carries a row
> of category pills above the catalog, one category loaded at a time, remembered for the session.

Run on 2026-09-10 against the real application, real SQLite file and a real HTTP server.
Tests passing is not evidence that the thing works, so this records what was actually observed.

## How it was run

```
rm -rf src/Host/CoreRentalNet.Host/App_Data
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5199 dotnet run
```

## Observed

| Check | Result |
|---|---|
| `GET /` | 200, hero, both featured cards, three "How it Works" steps |
| `GET /builder` | 200, selection panel with the four category tabs and 10 chairs, all seven slots rendered, empty-slot invitations labelled |
| `GET /extras` | 200, breadcrumb, Extras tab, 20 products |
| `GET /review` with an empty workspace | **302 → `/builder?empty=1`** — the gate works |
| `GET /review` with one chair assigned | 200; line "Seminyak Lounge" Qty 1, `Rp400.000/mo`, delivery `Rp750.000`, Grand Total `Rp1.150.000`, address echoed, both zone tiles |
| Draft cookie | `corerental.draft=…; max-age=2592000; path=/; samesite=lax; httponly` |
| Database | tables `Workspace_Draft`, `Workspace_SlotAssignment` only (plus EF's own); `pragma journal_mode` = **wal** |
| Drafts created | exactly **1** after several page loads, so starting a draft is genuinely idempotent |

## What running it caught that a green build did not

1. **The draft was never created.** The middleware issues a token, but nothing created the
   workspace, so the first read threw `NotFoundException` and every page returned 500. Fixed by
   adding an explicit, idempotent `StartDraft` command. This is a design gap, not a typo: no
   decision in the specs said who creates a draft.
2. **Only five of the seven slots rendered.** Lamp and Plant had geometry in `slots.css` and no
   component ever rendered them, so two slots were silently invisible. Fixed, and
   `SlotRenderingTests` now fails if any `SlotId` is not rendered.
3. **The database directory was never created** and EF's connection string does not create it,
   so start-up failed with "unable to open database file" on a clean checkout.

## Deliberate state at the end of E3

- **"Rent This Setup" is disabled on purpose.** Order creation belongs to the checkout change,
  and a button that looks clickable and does nothing is worse than one that says so. It says so.
- **`CheckoutSettings` is a provisional home for the delivery fee**, read from configuration
  (`Checkout:DeliveryFeeAmount`). The charge belongs to the invoice model and moves there with
  the Rentals module; it is in configuration rather than in markup so the number has one home.
- **`Secure` on the draft cookie is set only over HTTPS**, which is correct behaviour rather
  than a shortfall: over plain HTTP locally it is absent. The E2E suite must expect it only on
  an HTTPS origin.

## Not verified here

Anything requiring a browser: clicking, quantity changes, the picker dialog, reload and
two-tab behaviour, keyboard navigation and the mobile viewport. Those are E7's job and are
marked in `test-matrix.md` as manually observed rather than passing.
