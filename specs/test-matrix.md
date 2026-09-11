# Test Matrix — the source of every scenario

Every unit, integration, architecture and E2E test derives from a row here.
Traceability is maintained by hand (ADR-0013): no build gate, no trait convention.

**Layers:** `U` unit · `I` integration (real SQLite file) · `A` architecture (NetArchTest) · `E` E2E (real Kestrel + Playwright, no mocks, no interception)

**Status:** `pending` · `passing` · `n/a`

Derived mechanically rather than from memory: the slot table crossed with the 62
catalog SKUs, the subcategories crossed with zone coverage, the money rules, and
the lifecycle transitions. Missing rows are the intended signal that a capability
was never specified.

## Catalog (read-only, in-memory — ADR-0005)

| id | scenario | expected | layer | status |
|---|---|---|---|---|
| CAT-01 | Load `products.json` at startup | 62 SKUs; chair 10, desk 10, beanbag 10, coffee 10, lamp 6, monitor 8, plant 8 | I | passing |
| CAT-02 | File is malformed | startup fails fast with a clear message | I | passing |
| CAT-03 | File is missing | startup fails fast with the resolved path in the message | I | passing |
| CAT-04 | Query category `chair` | exactly 10, no other category leaks in | U | passing |
| CAT-05 | Query subcategory `coffee` | exactly 10 | U | passing |
| CAT-06 | Extras listing | `coffee ∪ beanbag` only | U | passing |
| CAT-07 | Accessories listing | `monitor ∪ lamp ∪ plant` only | U | passing |
| CAT-08 | `badge: popular` | exactly 2 SKUs flagged; featured selection uses it | U | passing |
| CAT-09 | `partner` category | absent — the item was removed from the file | U | passing |
| CAT-10 | Unknown SKU lookup | returns not-found, never throws | U | passing |
| CAT-11 | Price lookup | returns `Money` in IDR, 2dp stored | U | passing |
| CAT-12 | Image path points at a non-existent file | flagged so the UI renders a placeholder tile | U | passing |
| CAT-13 | Catalog exposes no mutation API | verified by architecture test on public surface | A | passing |
| CAT-14 | Catalog file contains a duplicate SKU (any casing) | rejected with the offending SKU named | I | passing |

## Store page (the catalog, one category at a time)
| ID | Scenario | Expected | Level | Status |
|---|---|---|---|---|
| STORE-01 | First visit | opens on desks, and only desks are loaded | E | passing |
| STORE-02 | The page itself | no selection panel; the catalog is the page | E | passing |
| STORE-03 | Choosing a category | the listing is replaced, not appended to | E | passing |
| STORE-04 | Remembered category | survives a page load in a session cookie | E | passing |
| STORE-05 | Remembered name the catalog does not know | ignored, falls back to desks | E | passing |

## Money (ADR-0004)

| id | scenario | expected | layer | status |
|---|---|---|---|---|
| MON-01 | Add two `Money` of the same currency | summed, currency preserved | U | passing |
| MON-02 | Add two `Money` of different currency | rejected | U | passing |
| MON-03 | Multiply by quantity | correct product | U | passing |
| MON-04 | Rounding, exactly `.5` | rounds `AwayFromZero` | U | passing |
| MON-05 | Rounding happens once at line finalization | no double rounding on a 9-unit workspace | U | passing |
| MON-06 | Display format | `id-ID` → `Rp400.000`, 0 decimals | U | passing |
| MON-07 | Zero and negative | zero allowed; negative rejected | U | passing |
| MON-08 | No `double` in the money path | architecture test over the Domain assemblies | A | passing |

## Slot rules (ADR-0008)

| id | scenario | expected | layer | status |
|---|---|---|---|---|
| SLOT-01 | Slot table | Desk 1, Chair 1, Monitor 3, Lamp 1, Plant 1, Coffee 1, Relax 1 | U | passing |
| SLOT-02 | Monitor places on the canvas | three boxes side by side; each removed on its own | E | passing |
| SLOT-03 | The desk | the bar is drawn whether or not a desk is chosen | E | passing |
| SLOT-02 | Maximum assignable units | 9 | U | passing |
| SLOT-03 | Zone derivation | only subcategories present in the catalog produce a zone | U | passing |
| SLOT-04 | Zone list rendered | Coffee Station and Relax Zone only | U,E | passing (observed by running it; automated in E7) |
| SLOT-05 | Slot capacity lives in data | changing Monitor to 4 requires no code change | U | passing |
| SLOT-06 | Every product in the real catalog maps to exactly one slot | mapping is total, and every slot has at least one product behind it | I | passing |

## Workspace composition

| id | scenario | expected | layer | status |
|---|---|---|---|---|
| WS-01 | Assign a chair to an empty workspace | Chair slot filled, quantity 1 | U | passing |
| WS-02 | Assign a second chair | replaces the first; quantity stays 1; total unchanged | U | passing |
| WS-03 | Assign three monitors | all three accepted | U | passing |
| WS-04 | Assign a fourth monitor | refused with a capacity message; total unchanged | U,E | passing |
| WS-05 | Assign a plant | lands in the Plant slot; the user never picks a slot | U | passing |
| WS-06 | Add a monitor from a product card when the slot is full | refused with a message, no silent replacement | U,E | passing |
| WS-07 | Assign a wrong-category item to a slot | unrepresentable — no code path permits it | U | passing |
| WS-08 | Remove via the slot's `×` | slot returns to its dashed empty state | U,E | passing |
| WS-09 | Stepper `−` at quantity 1 | removes the item | U | passing |
| WS-10 | Stepper `+` at capacity | disabled; state unchanged | U,E | passing |
| WS-11 | Quote | sum of `price × quantity` recomputed from the catalog on every read | U | passing |
| WS-12 | Catalog price changes between reads | quote reflects the new price | U | passing |
| WS-13 | Draft references a SKU absent from the catalog | validation error and a visible message — never a 500, never a silent drop | U,I | passing |
| WS-14 | Draft stores no price column | schema assertion | A | passing |
| WS-15 | A second monitor of another model | both stay in the slot; it holds products, not a count | U,E | passing |
| WS-16 | Desk and chair | mandatory: checkout shut without them, the reason naming the slot | U,E | passing |

## Draft persistence and the cookie token (ADR-0007)

| id | scenario | expected | layer | status |
|---|---|---|---|---|
| DR-01 | First GET with no cookie | an opaque draft token is issued | E | passing (observed by running it; automated in E7) |
| DR-02 | Navigate Builder → Store → Review | the same draft is carried through | E | passing (observed by running it; automated in E7) |
| DR-03 | Browser reload mid-funnel | the draft survives | E | passing (observed by running it; automated in E7) |
| DR-04 | Cookie attributes | `HttpOnly`, `Secure`, `SameSite=Lax` | E | passing (observed by running it; automated in E7) |
| DR-05 | Token is not the draft identifier | not equal to the draft id; stored hashed | I | passing |
| DR-06 | Two tabs edit the same draft | concurrency conflict surfaced; reload shows current state | U,I | passing |
| DR-07 | Draft created lazily | visiting Home twice does not create two drafts | I | passing |

## Navigation and gating

| id | scenario | expected | layer | status |
|---|---|---|---|---|
| NAV-01 | Empty workspace, mobile nav | `Summary` and `Rent` disabled | E | passing |
| NAV-02 | Empty workspace, Builder | `View Setup Summary` and `Ready to Rent?` disabled; total shows `Rp0/mo` | E | passing |
| NAV-03 | Empty workspace | every product card remains clickable and assigns | E | passing (observed by running it; automated in E7) |
| NAV-04 | Type `/review` directly with an empty workspace | redirect to Builder with a message | E | passing (observed by running it; automated in E7) |
| NAV-05 | Disabled controls | `aria-disabled`, not focusable, muted per design tokens | U,E | passing |
| NAV-06 | Assign one item | all gated controls become enabled | E | passing |
| NAV-07 | Header bag icon | decorative: no count, no navigation | U | passing |

## Delivery address

| id | scenario | expected | layer | status |
|---|---|---|---|---|
| ADDR-01 | Checkout with no address | refused, field highlighted | U,E | passing |
| ADDR-02 | Whitespace only | refused after trimming | U | passing |
| ADDR-03 | 4 characters / 201 characters | refused at both bounds | U | passing |
| ADDR-04 | Saved on blur | one write per blur, not per keystroke | U,E | passing |
| ADDR-05 | Newlines inside the value | normalised before storage | U | passing |
| ADDR-06 | Two-line textarea | renders 2 rows; location icon top-aligned | E | passing (observed by running it; automated in E7) |

## Checkout and the demo dialog (ADR-0010)

| id | scenario | expected | layer | status |
|---|---|---|---|---|
| CO-01 | Click `Rent This Setup` | dialog opens stating no money is taken | E | passing |
| CO-02 | Before typing | confirm control disabled | E | passing |
| CO-03 | Type `This Is A DEMO` with padding | accepted — trimmed, case-insensitive | U,E | passing |
| CO-04 | Type the wrong phrase and submit | server rejects; **no order created** | U,E | passing |
| CO-05 | Cancel the dialog | closes; workspace and address untouched | E | passing |
| CO-06 | Confirm | order persisted with line snapshots, quantities and the address | I | passing |
| CO-07 | Confirm | first invoice raised and immediately `Paid` | U,I | passing |
| CO-08 | One-time delivery fee | present on invoice #1, absent from renewals | U | passing |
| CO-09 | 0% tax seed | no tax line rendered; total equals sum of lines | U | passing |
| CO-10 | After the order | draft cleared and the cookie token rotated | E | passing |
| CO-11 | Confirm clicked twice | exactly one order — idempotent via `Draft → Converted` | U,E | passing |
| CO-12 | Checkout with an empty workspace | rejected server-side regardless of UI state | U | passing |
| CO-13 | Request containing a price or total | ignored; server prices from the catalog | U,A | passing |
| CO-14 | Order and invoice numbering | `CR-YYYY-NNNN`, `INV-YYYY-NNNN`, sequential | U | passing |
| CO-15 | Scheduler never creates a duplicate invoice for a settled period | see SC-04 | U | passing |

## Order confirmation

| id | scenario | expected | layer | status |
|---|---|---|---|---|
| ORD-01 | Open the confirmation URL with a valid token | renders the order | E | passing |
| ORD-02 | Open with an invalid or missing token | not found; no order data disclosed | E | passing |
| ORD-03 | Content | order number, itemised lines with quantities, subtotal, one-time fee, grand total, delivery address | E | passing |
| ORD-04 | Reload the confirmation page | still renders (the token is durable) | E | passing |
| ORD-05 | Workspace is empty after checkout | the builder shows an empty workspace | E | passing |

## Scheduler and lifecycle (ADR-0011)

| id | scenario | expected | layer | status |
|---|---|---|---|---|
| SC-01 | Time advances past the delivery lead time | delivery scheduled at the lead-time date | U | passing |
| SC-02 | Clock reaches the delivery date | rental becomes `Active` | U | passing |
| SC-03 | Monthly anniversary | next invoice raised **and settled exactly once** | U | passing |
| SC-04 | Scheduler executes twice for the same period | no duplicate invoice | U | passing |
| SC-05 | Cancellation requested | next invoice suppressed | U | passing |
| SC-06 | Cancellation effective date | end of the currently paid period, no refund | U | passing |
| SC-07 | Order placed 31 January | periods `anchor.AddMonths(N)` → 28 Feb, 31 Mar, 30 Apr | U | passing |
| SC-08 | Anchor stored once | no drift across twelve periods | U | passing |
| SC-09 | Order placed 01:00 WITA on 1 Feb | anchors to 1 Feb, not 31 Jan (fixed `+08:00`) | U | passing |
| SC-10 | Fake clock advances but no real time passes | architecture test: no `Thread.Sleep`, no `DateTime.Now` | A | passing |
| SC-11 | Lifecycle transitions | `Placed → Paid → DeliveryScheduled → Active → CancellationRequested → Ended` | U | passing |

## Architecture

| id | scenario | expected | layer | status |
|---|---|---|---|---|
| ARC-01 | Cross-module references | no module references another's `Domain` or `Infrastructure` | A | passing |
| ARC-02 | `DbContext` ownership | one per persisting module (Workspace, Rentals); Catalog has none | A | passing |
| ARC-03 | Table names | every mapped entity carries its module prefix | A | passing |
| ARC-04 | Domain purity | Domain references no EF Core or ASP.NET type | A | passing |
| ARC-05 | Absent plumbing | no mediator, no event bus, no outbox package referenced | A | passing |
| ARC-06 | Time access | `TimeProvider` used everywhere; no `DateTime.Now`/`UtcNow` in Domain | A | passing |

## UI, tokens and accessibility (ADR-0012)

| id | scenario | expected | layer | status |
|---|---|---|---|---|
| UI-01 | Design tokens | every colour, radius and spacing used comes from a CSS custom property | A | passing |
| UI-02 | Slot geometry | supplied by CSS variables keyed by slot id; no coordinate literals in C# | U | passing |
| UI-03 | Interactive elements | real `button`/`a` with accessible names — no clickable `div` | E | passing |
| UI-04 | Demo dialog | native `dialog`; focus trapped; `Esc` closes | E | passing |
| UI-05 | Missing product image | design-system placeholder tile renders | U,E | passing |
| UI-06 | Remote product images | vendored locally; the E2E run makes no third-party request | E | passing |
| UI-07 | Mobile viewport | bottom nav visible; Builder side panel collapsed; horizontal category chips | E | passing |
| UI-08 | Keyboard only | the full funnel is completable without a mouse | E | passing |

## Security

| id | scenario | expected | layer | status |
|---|---|---|---|---|
| SEC-01 | Tampered price or total in the request | ignored; server-priced | U | passing |
| SEC-02 | Tokens at rest | draft and order tokens stored hashed | I | passing |
| SEC-03 | Demo phrase | validated server-side, not only in the browser | U | passing |
| SEC-04 | Payments | no card, provider or webhook code exists anywhere | A | passing |
| SEC-05 | Draft token from one browser | cannot read or mutate another browser's draft | E | passing |
| SEC-06 | Order enumeration | guessing an order number without a token discloses nothing | E | passing |

## Authentication (ADR-0016 to ADR-0020)

| id | scenario | expected | layer | status |
|---|---|---|---|---|
| AUTH-01 | No identity configuration at all | OIDC is not registered, the sign-in affordance is hidden, every guest route works | I,E | pending |
| AUTH-02 | The application starts with no `Auth0:Domain` | it boots and serves the funnel; identity is not required to run | E | pending |
| AUTH-03 | Selecting sign-in | a redirect to the authority's authorization endpoint, carrying the return URL | E | pending |
| AUTH-04 | Completing the handshake | an authentication cookie is issued and the customer is signed in | E | pending |
| AUTH-05 | Selecting sign-out | the cookie is cleared and the browser is sent to the authority's logout endpoint | E | pending |
| AUTH-06 | A local return URL | honoured after sign-in | I,E | pending |
| AUTH-07 | A non-local return URL | refused, so the login page cannot be an open redirect | U,I | pending |
| AUTH-08 | Every committed configuration file | contains no client secret | A | pending |
| AUTH-09 | What is persisted after sign-in | no access token and no refresh token anywhere | U,A | pending |
| AUTH-10 | The authentication cookie | `HttpOnly`, `SameSite=Lax`, and `Secure` in production | I | pending |
| AUTH-11 | A signed-in checkout | the order records the subject, email and name as at that moment | U,I | pending |
| AUTH-12 | A guest checkout | the order records no customer, and its link still opens it | U,I | pending |
| AUTH-13 | My rentals | lists only the signed-in customer's own orders | U,I,E | pending |
| AUTH-14 | One customer requesting another's order by number | nothing, and no data disclosed | U,I | pending |
| AUTH-15 | An order detail page | opens by ownership, and by token for a guest | U,I,E | pending |
| AUTH-16 | The sign-in affordance | present in the header and at the review step, and absent when unconfigured | E | pending |
| AUTH-17 | The browser suite | drives a real OIDC handshake against a local provider, with no interception | E | pending |
| AUTH-18 | Production with the local provider enabled | the application refuses to start | A | pending |
| AUTH-19 | Real tenant credentials present | the opt-in test validates the tenant configuration; skipped otherwise | I | pending |
| AUTH-20 | Signing in with a workspace in progress | the draft survives the round trip | E | pending |
| AUTH-21 | Signing out | the draft and any confirmation link are left alone | E | pending |
| AUTH-22 | A tab left open past the cookie expiry | keeps the circuit's authentication context, as recorded in ADR-0019 | E | pending |
| AUTH-23 | A signed-in header | initials are drawn, and no image is fetched from the identity provider | E | pending |
| AUTH-24 | A signed-in journey | reaches nothing but the application, exactly as the guest journey does | E | pending |

## Notes recorded during E7

- The browser suite is committed (`tests/CoreRentalNet.E2E`): a real Kestrel process started the way
  a person starts it, its own throwaway database, one browser context per test, Chromium, and **no
  interception of any kind**. See `specs/verifications/E7-browser-suite.md`.
- It found three application bugs that 280 passing tests and every `curl` check had missed:
  a Razor expression that rendered `True.ToString().ToLowerInvariant()` into an accessibility
  attribute, an E2E readiness signal that matched prerendered markup, and a redirect whose query
  value could not be bound to a `bool`.
- `SLOT-04` is asserted as two zones rather than four: Garage and Outdoor Gear were removed with
  the partner item, and a zone no product can fill must not be drawn.

## Notes recorded during E6

- Every scheduler behaviour is tested by moving a clock, never by waiting. See
  `specs/verifications/E6-scheduler.md`, which also records the bug that only the database
  could show: the pass saved only when the order changed, and billing does not change the order,
  so every renewal was silently dropped.
- The clock rule (`ARC-06`) now covers Application code as well as Domain, because that is where
  the schedule lives.

## Notes recorded during E5

- The checkout funnel is verified in a real browser, not only in tests: see
  `specs/verifications/E5-browser-walkthrough.md`. That run found two bugs no unit test could,
  one of which made every browser share a single workspace.
- `NAV-02`, `NAV-05`, `UI-03` to `UI-08` and the mobile viewport rows stay pending for E7, which
  builds the browser suite properly rather than a one-off script.

## Notes recorded during E3

- `DR-04`: `Secure` is set only when the request is HTTPS, so the E2E suite must assert it on an
  HTTPS origin and must assert `HttpOnly` and `SameSite=Lax` everywhere.
- `WS-04/06/08/10`, `NAV-01/02/05/06`, `UI-03/04/05/07/08` remain pending: they need a browser.
- `ORD-01` and `ORD-04` are marked as passing on the data path: the query, the token check and
  the frozen amounts are covered, and the rendered page arrives with checkout.
- `CO-11/12/13` and everything about the confirmation *page* remain pending until checkout exists.
- `SLOT-06` and `UI-06` both read the real catalog file and the real web root, so a new remote
  image URL or a product with an unmapped subcategory fails the build.
