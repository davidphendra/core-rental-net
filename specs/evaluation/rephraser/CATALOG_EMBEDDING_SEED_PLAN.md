# Catalogue Embedding Seed Regeneration Plan

Status: proposed. No embedding seed has been regenerated in this step.

## Correct data flow

The previous plan incorrectly treated `discovery.db` as a product source.

Correct flow:

```text
catalog seeder
  -> catalog.db
       |
       v
CatalogIngestion / embedding generator
       |
       v
vector embedding SQLite file
       |
       v
CatalogIngestion --seed
       |
       v
SeedProductEmbeddings migration
```

The roles are:

| Artifact | Role |
|---|---|
| `catalog.db` | Product input. Produced by the catalogue seeder. |
| `Catalog_Product` | The rows read for embedding. |
| vector embedding SQLite file | Output of the embedding generator. |
| `SeedProductEmbeddings` migration | EF migration generated from the vector file. |

`catalog.db` is the product source.

The vector embedding file is not a product source.

`--seed` does not read `catalog.db`; it reads the vector embedding file and writes the EF migration.

## Current state

The catalogue rows and the embedding migration are out of sync.

```text
SeedProducts / Catalog_Product: updated
catalog.db:                     current catalogue rows
vector embedding file:          rebuilt
SeedProductEmbeddings:          stale
```

The stale embedding seed still contains the old recipe:

```text
modelId:       text-embedding-3-small
width:         384
composition:   name+description+metadata/1
catalogueHash: 7f50a4f755b2df85f739e61ed20a4b4cb38f98e7908bb42a69d192c8dd41539a
```

The current embedding output is expected to contain:

```text
modelId:       all-MiniLM-L6-v2-embedding
width:         384
composition:   name+description+metadata/1
catalogueHash: 5c683736c12426e9ebe6913e6196e1b115d13489d60020e491ca0573fbb71b3a
vectors:       637
```

## Prerequisites

1. The catalogue seeder must have produced the current `catalog.db`.
2. `Catalog_Product` must contain the current rows and current metadata.

```text
products: 205
```

3. The catalogue hash must match the rows used for embedding.
4. The vector embedding SQLite output must be built from that `catalog.db`, not from `products.json`.

If `catalog.db` changes, regenerate the vector file before regenerating the EF seed.

## Code correction required

Current `CatalogIngestion` reads `products.json` through `CatalogueFile.Read(...)`.

That is not the intended product source for this plan.

Required change:

```text
Catalog:FilePath (JSON)  ->  Catalog:DatabasePath (SQLite)
```

Add a catalogue SQLite reader that reads:

```text
Catalog_Product
  Sku, Name, Category, SubCategory, Amount, Currency,
  Description, Image, Tags, BestFor, NotFor, Attributes
```

and builds the same domain `Product` records the embedding renderer already consumes.

Do not add a second definition of valid product metadata. Reuse the catalogue mapper where possible.

Do not read `discovery.db` as a product input.

## Vector output

The embedding generator writes a vector embedding SQLite file.

In this repository, the current name of that output is:

```text
src/Host/CoreRentalNet.Host/App_Data/discovery.db
```

That name is an implementation detail. The important rule is:

```text
catalog.db is input
vector embedding SQLite file is output
```

`--seed` reads the vector output. It does not read `catalog.db`.

If the final design requires a name that communicates the role, rename the output to something such as:

```text
catalog-embeddings.db
```

Do not leave `discovery.db` looking like a second catalogue source.

## Regeneration sequence

1. Verify `catalog.db`:

   ```text
   Catalog_Product rows: 205
   current metadata present
   current catalogue hash recorded by the embedding run
   ```

2. Change `CatalogIngestion` to read `catalog.db`.

3. Build the tool.

4. Run the embedding generator:

   ```bash
   dotnet run --project src/Tools/CoreRentalNet.CatalogIngestion
   ```

   This step:

   - reads `catalog.db`;
   - renders product text;
   - calls the local embedding server;
   - writes the vector embedding SQLite file;
   - records model, width, composition, and catalogue hash.

5. Verify the vector output:

   ```text
   vector count: 637
   model: all-MiniLM-L6-v2-embedding
   width: 384
   composition: name+description+metadata/1
   catalogue hash equals catalog.db hash
   ```

6. Confirm exactly one embedding migration shell exists:

   ```text
   src/Modules/Discovery/CoreRentalNet.Modules.Discovery.Infrastructure/Migrations/
     *_SeedProductEmbeddings.cs
   ```

7. Generate the EF seed from the vector output:

   ```bash
   dotnet run --project src/Tools/CoreRentalNet.CatalogIngestion -- --seed
   ```

   Seed mode:

   - reads the vector embedding SQLite file;
   - does not read `catalog.db`;
   - does not call an embedding server;
   - overwrites the `*_SeedProductEmbeddings.cs` migration;
   - leaves EF’s migration timestamp, Designer partial, and model snapshot intact.

8. Verify the regenerated migration:

   ```text
   modelId: all-MiniLM-L6-v2-embedding
   width: 384
   composition: name+description+metadata/1
   catalogueHash: current vector file hash
   vector rows: 637
   ```

9. Apply the migration to a clean discovery database.

10. Verify:

    ```text
    Discovery_ProductVector rows: 637
    Discovery_ProductVectorRecipe matches the vector file
    CatalogIndexFreshnessService accepts the index
    host starts with the clean database
    ```

11. Run the catalogue retrieval probe before any agent evaluation.

## Acceptance checks

```text
catalog.db has 205 current products
embedding generator reads catalog.db, not products.json
embedding output has 637 vectors
output recipe model == all-MiniLM-L6-v2-embedding
output width == 384
output composition == name+description+metadata/1
output catalogue hash == catalog.db-backed embedding hash
SeedProductEmbeddings matches the vector output
fresh database applies the migration
fresh database vector count == 637
host freshness check passes
catalogue retrieval probe passes expected-SKU checks
```

## Do not do

- Do not use `discovery.db` as a product source.
- Do not embed `products.json` when the plan requires `catalog.db`.
- Do not regenerate the EF seed from a stale vector file.
- Do not apply the old `text-embedding-3-small` seed to a new database.
- Do not hand-edit the generated migration.
- Do not change model or width without rebuilding vectors and seed.
- Do not run a full agent evaluation before the clean-database checks pass.

## Files affected

```text
src/Tools/CoreRentalNet.CatalogIngestion/
  ConfigurationReader.cs
  IngestionSettings.cs
  CatalogueFile.cs or a new catalog.db reader
  appsettings.json

src/Modules/Discovery/CoreRentalNet.Modules.Discovery.Infrastructure/Migrations/
  *_SeedProductEmbeddings.cs
```

The Designer partial and model snapshot remain EF-owned.
