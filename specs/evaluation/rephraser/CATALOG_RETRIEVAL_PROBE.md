# Catalogue Retrieval Probe

A separate catalogue-only probe was not run before the half evaluation. The half evaluation provides the first end-to-end measurement after the overlay.

## Current half result

| Measure | Strict value |
|---|---:|
| `raw_retrieval_recall` | 73/79 = 92.4% |
| `retrieval_quality` | 56/79 = 70.9% |
| Expected SKU missing (`RETRIEVAL_MISS`) | 37 occurrences |
| `NO_CANDIDATE` | 26 cases |

This is a strong raw-recall improvement over the previous half run (69/86 = 80.2%), but it is not a substitute for the planned per-term catalogue probe.
