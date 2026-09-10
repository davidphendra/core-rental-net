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
| SLOT-02 | Maximum assignable units | 9 | U | passing |
| SLOT-03 | Zone derivation | only subcategories present in the catalog produce a zone | U | passing |
| SLOT-04 | Zone list rendered | Coffee Station and Relax Zone only | U,E | pending |
| SLOT-05 | Slot capacity lives in data | changing Monitor to 4 requires no code change | U | passing |
| SLOT-06 | Every product in the real catalog maps to exactly one slot | mapping is total, and every slot has at least one product behind it | I | passing |

## Workspace composition

| id | scenario | expected | layer | status |
|---|---|---|---|---|
| WS-01 | Assign a chair to an empty workspace | Chair slot filled, quantity 1 | U | passing |
| WS-02 | Assign a second chair | replaces the first; quantity stays 1; total unchanged | U | passing |
| WS-03 | Assign three monitors | all three accepted | U | passing |
| WS-04 | Assign a fourth monitor | refused with a capacity message; total unchanged | U,E | passing (unit; E2E pending) |
| WS-05 | Assign a plant | lands in the Plant slot; the user never picks a slot | U | passing |
| WS-06 | Add a monitor from a product card when the slot is full | refused with a message, no silent replacement | U,E | passing (unit; E2E pending) |
| WS-07 | Assign a wrong-category item to a slot | unrepresentable — no code path permits it | U | passing |
| WS-08 | Remove via the slot's `×` | slot returns to its dashed empty state | U,E | passing (unit; E2E pending) |
| WS-09 | Stepper `−` at quantity 1 | removes the item | U | passing |
| WS-10 | Stepper `+` at capacity | disabled; state unchanged | U,E | passing (unit; E2E pending) |
| WS-11 | Quote | sum of `price × quantity` recomputed from the catalog on every read | U | passing |
| WS-12 | Catalog price changes between reads | quote reflects the new price | U | passing |
| WS-13 | Draft references a SKU absent from the catalog | validation error and a visible message — never a 500, never a silent drop | U,I | passing |
| WS-14 | Draft stores no price column | schema assertion | A | passing |

## Draft persistence and the cookie token (ADR-0007)

| id | scenario | expected | layer | status |
|---|---|---|---|---|
| DR-01 | First GET with no cookie | an opaque draft token is issued | E | pending |
| DR-02 | Navigate Builder → Store → Review | the same draft is carried through | E | pending |
| DR-03 | Browser reload mid-funnel | the draft survives | E | pending |
| DR-04 | Cookie attributes | `HttpOnly`, `Secure`, `SameSite=Lax` | E | pending |
| DR-05 | Token is not the draft identifier | not equal to the draft id; stored hashed | I | passing |
| DR-06 | Two tabs edit the same draft | concurrency conflict surfaced; reload shows current state | U,I | passing |
| DR-07 | Draft created lazily | visiting Home twice does not create two drafts | I | pending |

## Navigation and gating

| id | scenario | expected | layer | status |
|---|---|---|---|---|
| NAV-01 | Empty workspace, mobile nav | `Summary` and `Rent` disabled | E | pending |
| NAV-02 | Empty workspace, Builder | `View Setup Summary` and `Ready to Rent?` disabled; total shows `Rp0/mo` | E | pending |
| NAV-03 | Empty workspace | every product card remains clickable and assigns | E | pending |
| NAV-04 | Type `/review` directly with an empty workspace | redirect to Builder with a message | E | pending |
| NAV-05 | Disabled controls | `aria-disabled`, not focusable, muted per design tokens | U,E | pending |
| NAV-06 | Assign one item | all gated controls become enabled | E | pending |
| NAV-07 | Header bag icon | decorative: no count, no navigation | U | pending |

## Delivery address

| id | scenario | expected | layer | status |
|---|---|---|---|---|
| ADDR-01 | Checkout with no address | refused, field highlighted | U,E | pending |
| ADDR-02 | Whitespace only | refused after trimming | U | passing |
| ADDR-03 | 4 characters / 201 characters | refused at both bounds | U | passing |
| ADDR-04 | Saved on blur | one write per blur, not per keystroke | U,E | pending |
| ADDR-05 | Newlines inside the value | normalised before storage | U | passing |
| ADDR-06 | Two-line textarea | renders 2 rows; location icon top-aligned | E | pending |

## Checkout and the demo dialog (ADR-0010)

| id | scenario | expected | layer | status |
|---|---|---|---|---|
| CO-01 | Click `Rent This Setup` | dialog opens stating no money is taken | E | pending |
| CO-02 | Before typing | confirm control disabled | E | pending |
| CO-03 | Type `This Is A DEMO` with padding | accepted — trimmed, case-insensitive | U,E | pending |
| CO-04 | Type the wrong phrase and submit | server rejects; **no order created** | U,E | pending |
| CO-05 | Cancel the dialog | closes; workspace and address untouched | E | pending |
| CO-06 | Confirm | order persisted with line snapshots, quantities and the address | I | pending |
| CO-07 | Confirm | first invoice raised and immediately `Paid` | U,I | pending |
| CO-08 | One-time delivery fee | present on invoice #1, absent from renewals | U | pending |
| CO-09 | 0% tax seed | no tax line rendered; total equals sum of lines | U | pending |
| CO-10 | After the order | draft cleared and the cookie token rotated | E | pending |
| CO-11 | Confirm clicked twice | exactly one order — idempotent via `Draft → Converted` | U,E | pending |
| CO-12 | Checkout with an empty workspace | rejected server-side regardless of UI state | U | pending |
| CO-13 | Request containing a price or total | ignored; server prices from the catalog | U,A | pending |
| CO-14 | Order and invoice numbering | `CR-YYYY-NNNN`, `INV-YYYY-NNNN`, sequential | U | pending |
| CO-15 | Scheduler never creates a duplicate invoice for a settled period | see SC-04 | U | pending |

## Order confirmation

| id | scenario | expected | layer | status |
|---|---|---|---|---|
| ORD-01 | Open the confirmation URL with a valid token | renders the order | E | pending |
| ORD-02 | Open with an invalid or missing token | not found; no order data disclosed | E | pending |
| ORD-03 | Content | order number, itemised lines with quantities, subtotal, one-time fee, grand total, delivery address | E | pending |
| ORD-04 | Reload the confirmation page | still renders (the token is durable) | E | pending |
| ORD-05 | Workspace is empty after checkout | the builder shows an empty workspace | E | pending |

## Scheduler and lifecycle (ADR-0011)

| id | scenario | expected | layer | status |
|---|---|---|---|---|
| SC-01 | Time advances past the delivery lead time | delivery scheduled at the lead-time date | U | pending |
| SC-02 | Clock reaches the delivery date | rental becomes `Active` | U | pending |
| SC-03 | Monthly anniversary | next invoice raised **and settled exactly once** | U | pending |
| SC-04 | Scheduler executes twice for the same period | no duplicate invoice | U | pending |
| SC-05 | Cancellation requested | next invoice suppressed | U | pending |
| SC-06 | Cancellation effective date | end of the currently paid period, no refund | U | pending |
| SC-07 | Order placed 31 January | periods `anchor.AddMonths(N)` → 28 Feb, 31 Mar, 30 Apr | U | pending |
| SC-08 | Anchor stored once | no drift across twelve periods | U | pending |
| SC-09 | Order placed 01:00 WITA on 1 Feb | anchors to 1 Feb, not 31 Jan (fixed `+08:00`) | U | pending |
| SC-10 | Fake clock advances but no real time passes | architecture test: no `Thread.Sleep`, no `DateTime.Now` | A | pending |
| SC-11 | Lifecycle transitions | `Placed → Paid → DeliveryScheduled → Active → CancellationRequested → Ended` | U | pending |

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
| UI-01 | Design tokens | every colour, radius and spacing used comes from a CSS custom property | A | pending |
| UI-02 | Slot geometry | supplied by CSS variables keyed by slot id; no coordinate literals in C# | U | pending |
| UI-03 | Interactive elements | real `button`/`a` with accessible names — no clickable `div` | E | pending |
| UI-04 | Demo dialog | native `dialog`; focus trapped; `Esc` closes | E | pending |
| UI-05 | Missing product image | design-system placeholder tile renders | U,E | pending |
| UI-06 | Remote product images | vendored locally; the E2E run makes no third-party request | E | pending |
| UI-07 | Mobile viewport | bottom nav visible; Builder side panel collapsed; horizontal category chips | E | pending |
| UI-08 | Keyboard only | the full funnel is completable without a mouse | E | pending |

## Security

| id | scenario | expected | layer | status |
|---|---|---|---|---|
| SEC-01 | Tampered price or total in the request | ignored; server-priced | U | pending |
| SEC-02 | Tokens at rest | draft and order tokens stored hashed | I | pending |
| SEC-03 | Demo phrase | validated server-side, not only in the browser | U | pending |
| SEC-04 | Payments | no card, provider or webhook code exists anywhere | A | pending |
| SEC-05 | Draft token from one browser | cannot read or mutate another browser's draft | E | pending |
| SEC-06 | Order enumeration | guessing an order number without a token discloses nothing | E | pending |
