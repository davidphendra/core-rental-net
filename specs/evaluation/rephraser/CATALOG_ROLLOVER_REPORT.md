# Catalogue Rollover Report

## Applied overlay

Accepted SKUs: 9.

```text
MONB086Q776F  PLTB0BPRTB7C  LMPB072R2M9K  CHAB0BYXJBVW
DSKB07PFFFQ2  DSKB004DOXP7  DSKB09MDNYM3  CHAB0855B3K7
LMPB0BGHVFD1
```

Changes were applied to:

- `src/shared/data/products.json`
- `Catalog_Product` in `src/Host/CoreRentalNet.Host/App_Data/catalog.db`
- `20261016035430_SeedProducts.cs`

## Ingestion

`CoreRentalNet.CatalogIngestion` was run against the local embedding server.

```text
Products: 205
Vectors:  637
Previous: 638
```

The one-vector difference is recorded; the vector recipe and freshness check should be rechecked after any later catalogue edit.

## Downstream

The host was restarted with the rebuilt `discovery.db`; the agent was restarted with a fresh trace directory and the new prompt versions.
