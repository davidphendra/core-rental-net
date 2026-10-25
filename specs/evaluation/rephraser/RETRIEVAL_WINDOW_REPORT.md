# Retrieval Window Report

## Implemented

```text
CatalogToolLimits.DefaultProductCount: 8 -> 12
CatalogToolLimits.MaximumProductCount: 25 (unchanged)
WorkspaceComponentProductPoolPolicy: 15 (verified)
WorkspaceSuggestionWorkflowOptions.MaximumRetrievedProductsPerComponentForReranking: 15
MaximumSelectedProductsPerComponent: 3
RankedTermMerger.MaximumPerTerm: 25
```

Both `search_catalogue` and `search_similarity_catalogue` use the shared default.

## Half-run measurement

| Measure | Previous half, phi judge | Current half, gpt-oss judge |
|---|---:|---:|
| Raw pass | 58/100 | 68/100 |
| Strict pass | 58/86 | 68/79 |
| Timeouts | 14 | 21 |
| Exact raw | 58 | 50 |
| Raw retrieval recall strict | 69/86 = 80.2% | 73/79 = 92.4% |
| Retrieval quality strict | 58/86 = 67.4% | 56/79 = 70.9% |
| Mean wall time | 94.8 s | 130.7 s |
| Mean model time | 23.1 s | 33.6 s |
| Mean unaccounted time | 71.4 s | 96.8 s |

The wider tool window and facts projection increased input size and wall time. Retrieval recall improved strongly, but timeout count increased. The raw pass gain was +10; strict pass gain was +10; exact raw lost 8 because 13 previously exact cases became timeouts or were otherwise lost, while 5 previously timed-out cases became exact.
