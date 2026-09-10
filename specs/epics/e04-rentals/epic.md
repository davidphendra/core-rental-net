# E4 — Rentals (order, invoice, periods)

**Slice:** 1 · **Status:** ready · **ADR:** 0004, 0006, 0009, 0010, 0011

## Goal
The order that exists after confirmation: a rental with snapshotted lines, its
first invoice settled, and a hashed access token that the customer can return to.

## In scope
- `Rental` aggregate: `RentalNumber` (`CR-YYYY-NNNN`), status, hashed
  `AccessToken`, delivery-address snapshot, period anchor, line snapshots
  (SKU, name, unit monthly price, quantity), one-time delivery fee, cancellation
  fields.
- `Invoice` aggregate: `InvoiceNumber` (`INV-YYYY-NNNN`), sequential number
  source, lines, subtotal, tax (0% seed; line rendered only when non-zero),
  delivery fee **on the first invoice only**, total, status `Open → Paid`,
  issued/paid timestamps. **Amounts snapshotted at issue.**
- Policies: delivery lead time, renewal-on-anniversary, cancellation effective at
  the end of the paid period.
- `IPaymentGateway` with a single `DemoPaymentGateway` implementation that
  settles immediately. No provider, no webhook, no card data.
- Persistence: `RentalsContext`, `Rentals_*` tables, migrations, access-token
  generation and hashing.
- Application queries: get rental by token, get invoices by token.

## Out of scope
The dialog, the checkout orchestration and the confirmation page (E5). The time
driver (E6). Any refund, retry, dunning or suspension logic.

## Stories
1. As the business I want an order number a person can read aloud.
2. As the business I want invoice amounts frozen at issue, so a later catalog
   change cannot rewrite history.
3. As the business I want the delivery fee charged once and never on a renewal.
4. As a customer I want to return to my order using only the link I was given,
   with no account.
5. As a security reviewer I want access tokens stored hashed and to be opaque
   values rather than identifiers.

## Definition of done
Build clean; unit + integration tests green; matrix rows passing; archived.

## Matrix rows
CO-06 · CO-07 · CO-08 · CO-09 · CO-14 · ORD-01 · ORD-02 · ORD-04 · SC-11 ·
SEC-02 · SEC-06
