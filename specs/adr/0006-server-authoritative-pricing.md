# ADR-0006: Prices are server-authoritative; drafts hold references only

- **Status:** Accepted (2026-09-10)
- **Decided by:** architecture

## Context
The Builder displays a live monthly total. A client-computed total is a
tamperable total: any request could claim a chair costs Rp1.

## Decision
- The draft stores **SKU references and quantities only — never prices**.
- Every total is recomputed from the Catalog snapshot on each read.
- Amounts are **snapshotted when the invoice is issued**, so a later catalog
  change can never rewrite history.
- The client never sends a price, total or rate that influences a charge.
- The Workspace aggregate is authoritative over slot membership, category
  compatibility and capacity. The UI is a projection.
- Rentals obtains a **frozen composition** through a public contract, re-validates
  and prices it via Catalog's public contract, and never touches another module's
  tables.

## Consequences
Displayed total and charged total are produced by the same code path. A SKU that
disappears from the catalog makes the affected line unrereferenceable and is
reported rather than silently dropped or silently zero-priced.
