# Rephraser Replay Results

A standalone rephraser-only replay was not run before the half evaluation. The half run is the end-to-end result after the implemented prompt, catalogue, facts, and window changes.

## Current half result

```text
Raw pass:        68/100
Strict pass:     68/79 = 86.1%
Exact answer:    50/100 raw; 50/79 strict = 63.3%
Judge evaluated: 45
Judge passed:    18
Judge rejected:  27
Judge errors:     0
Timeouts:        21
```

The current run cannot isolate the rephraser contribution from the catalogue, facts, and window changes. A standalone replay remains required for attribution.
