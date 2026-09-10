# ADR-0018: The order carries a customer snapshot, not a customer entity

- **Status:** Accepted (2026-09-10)

## Context
An order now has an owner. The options were to store the identity provider's subject claim only and
fetch a profile when needed, to keep a local customer table in sync on each sign-in, or to write
the customer onto the order.

## Decision
Three nullable columns on `Rental`, written once at checkout:
`CustomerId` (the OIDC `sub`), `CustomerEmail` and `CustomerName`. A guest order leaves all three
null.

- `sub` is the identifier, never the email: an email address is a mutable attribute of an account,
  not a key.
- The name and email are a **snapshot as of the order**, which is what a delivery and a historical
  record need. No user table to keep in sync, and no call to the identity provider at read time.
- The email is recorded **whether or not it is verified**, and does not block checkout.

## Consequences
- One migration, one extra argument to the existing checkout command.
- An order remains self-describing if the identity provider account is later deleted.
- Deferring the local customer table costs a migration later if a profile page or roles ever
  arrive. That is a cheaper mistake than maintaining a synchronisation path now.
