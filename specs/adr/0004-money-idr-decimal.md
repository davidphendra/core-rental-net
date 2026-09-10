# ADR-0004: Money as an IDR decimal value object

- **Status:** Accepted (2026-09-10)
- **Decided by:** product owner (IDR), architecture (representation)

## Context
The catalog expresses prices as bare integers (100000–1500000) with no currency
field. The design mockups use USD with cent values. Bali rental business, billed
monthly in rupiah.

## Decision
- Single currency: **IDR only**, held as a domain constant. `Money` still carries
  a currency code so adding another later is additive.
- `Money` value object: `decimal Amount` + `string Currency`, stored
  `decimal(18,2)`, serialized `{ "amount": 400000, "currency": "IDR" }`.
- `double` is never admissible for money.
- **One rounding point**, at line finalization, `MidpointRounding.AwayFromZero`.
- Display via `id-ID`: `Rp400.000/mo`, 0 decimals, while internal arithmetic
  keeps 2dp for tax and any future proration.
- Mockup prices and mockup product names are discarded; `products.json` is the
  single source of truth for every amount.
- Tax rate is modelled but **seeded to 0%**; the tax line renders only when
  non-zero, so designed totals stay exact.

## Consequences
IDR has no cents in practice but 2dp is retained internally so enabling PPN does
not require a schema change.
