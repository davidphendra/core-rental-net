# Product currency in products.json — verification

`products.json` may now state the currency a price is in (`"currency": "IDR"`), defaulting to the
settlement currency when absent. The loader validates the code's ISO 4217 shape and refuses a price in
any currency the application cannot settle in, so a mismatch fails while the file is still in hand
instead of surfacing as a mixed-currency sum at checkout.

| Baseline | Before | After | Result |
|---|---|---|---|
| BUILD | 0 warnings, 0 errors | 0 warnings, 0 errors | unchanged |
| NONBROWSER | 488 passed, 0 failed | 492 passed, 0 failed | +4 new tests (2 mapping, 2 rules) |
| BROWSER | 103 passed, 1 skipped | 103 passed, 1 skipped | unchanged |

The separation this change enforces: an amount's currency is the amount's own (`Money.Currency`, read
from the catalog); `CultureInfo` only decides how that amount is written. The two stay in step because
the catalog must state the settlement currency, and `Culture:Name` (`en-ID`) is the culture that writes
it the way Denpasar reads it. `CommittedConfigurationTests` now says so out loud.

## What changed

- `Currencies.IsWellFormed` — the ISO 4217 shape test (three uppercase letters), a shape test rather
  than a membership test.
- `ProductJsonRecord` — reads the optional `currency` field.
- `ProductLoader` — builds `Money` from the stated currency (absent means `IDR`), normalises case,
  refuses an ill-formed code, and refuses a currency other than the settlement one, naming the SKU.
- `src/shared/data/products.json` — every row states `"currency": "IDR"`.
