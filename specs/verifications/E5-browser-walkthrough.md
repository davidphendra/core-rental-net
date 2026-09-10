# E5 verification — the funnel in a real browser

Run on 2026-09-10 with Playwright driving Chromium against the real application, two real
SQLite databases and the real catalog file. Nineteen checks, all passing.

Everything below is a behaviour that **only** a browser can test. The typed-phrase gate, the
dialog, the cookie rotation and the cart reset are all JavaScript and HTTP, so `curl` and a
green test suite prove nothing about them.

## What was exercised

| Check | Result |
|---|---|
| Two browsers, two carts | PASS — one browser assigning an item does not appear in the other |
| Assigning an item fills a slot | PASS |
| Review shows line items, quantity, delivery charge and grand total | PASS |
| Checkout with no delivery address | PASS — refused, with the message shown **inside** the open dialog |
| Dialog resets between openings | PASS — the phrase typed last time is cleared and confirm is disabled again |
| Confirm disabled before typing, and for a wrong phrase | PASS |
| Padded mixed case (`  This Is A DEMO `) accepted | PASS |
| Checkout redirects to the confirmation page | PASS |
| The reset flag is stripped by the middleware redirect | PASS |
| Confirmation shows order number, address, totals, "paid", and the no-payment notice | PASS |
| The cart is empty afterwards, and the other browser still has its own | PASS |
| Reopening the confirmation link still shows the order | PASS |
| A guessed token shows "not found" and leaks no address | PASS |

## Two bugs that only the browser found

1. **Every circuit shared one draft.** `App.razor` passed the draft token as
   `DraftToken="DraftToken"`. Razor treats an unquoted attribute value as a literal string, so
   every browser was handed the constant token `"DraftToken"` and therefore the same workspace.
   Consequence: the cart never reset, and two browsers shared a cart. Fixed by writing
   `DraftToken="@DraftToken"`.
2. **The dialog kept the previous phrase.** `ShowAsync` cleared the field but Blazor cannot see a
   private field change, so the control was still enabled with the old text when reopened. Fixed
   with an explicit `StateHasChanged`.

Also found: **cascading values do not cross into an interactive render boundary.** A value
declared by a statically-rendered parent is never serialized into the circuit, so the canvas
rendered nothing at first. The token now travels as a component parameter of the interactive
root, which *is* serialized, and is re-published as a cascading value inside the interactive
tree.

## Correction to the E3 verification

`E3-host-walkthrough.md` recorded the draft cookie rotating, one draft row, and the empty-workspace
redirect. Those observations were made while bug 1 was live, so the *cookie* behaviour was real but
the workspace behind it was shared. The E3 claims that depended on per-browser drafts are
re-verified here: two browsers now hold two distinct drafts, each matching its own cookie hash.

## Not verified here

The mobile viewport, keyboard-only completion and the picker dialog's focus behaviour are E7's.
`DR-04` asserts `Secure` on the cookie only over HTTPS, which this run is not.
