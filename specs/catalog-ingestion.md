# Catalog ingestion — spec

The requirement in flight. It describes the standalone ingestion tool: what it reads, how it chunks and
embeds, the one table it writes, and what it deliberately does not do. The decision record is
[`adr/0001-catalog-ingestion.md`](adr/0001-catalog-ingestion.md).

## Purpose

Build a vector table from `products.json` on a developer's machine, without a cloud credential, so that
each product is represented by several vectors — one per passage of what it says about itself — in a
SQLite file the tool owns.

## Scope

**In** — `src/Tools/CoreRentalNet.CatalogIngestion/`: reading the catalogue, chunking each product,
embedding the chunks through a local server, and writing the table.

**Out** — the retrieval module (`Discovery`) and the application: not read, not referenced, not changed.
Ranking, search and the suggester's use of this table are separate work. Also out: deploying or
provisioning anything.

## Pipeline

1. **Read** `Catalog:FilePath` (`products.json`) through the catalogue module's `ProductCatalog`.
2. **Render** each product with `EmbeddedText` — **name + description + the whole metadata** (tags,
   bestFor, notFor, every attribute), attributes ordered, empty sections omitted, `\n`-joined.
3. **Chunk** that text with `SemanticChunker.NET`, using the `Chunker:*` settings.
4. **Embed** each chunk through `Llm:Server` / `Llm:Model`.
5. **Check, then write.** Before the first write: the catalogue is non-empty, every product produced at
   least one chunk, and every vector is `Embedding:Width` wide. A failure writes nothing.
6. **Replace** the table in one transaction — `DELETE`, then every row. Re-running is idempotent.

## Configuration

`appsettings.json` beside the tool, overridable by a gitignored `appsettings.Local.json`. **An absent key
takes its default; a key present but unusable stops the run.** No command-line overrides; the only argument
is `--help`.

| Key | Default |
|---|---|
| `Llm:Server` | `http://localhost:8080/v1` |
| `Llm:Model` | `all-MiniLM-L6-v2-embedding` |
| `Embedding:Width` | `384` |
| `Catalog:FilePath` | `../../shared/data/products.json` (relative to the tool's project directory) |
| `Database:Path` | `App_Data/product_embedding.db` (relative to the tool's project directory) |
| `Chunker:TokenLimit` | `256` |
| `Chunker:BufferSize` | `1` |
| `Chunker:ThresholdType` | `Percentile` |
| `Chunker:ThresholdAmount` | `95` |
| `Chunker:TargetChunkCount` | *(unset — the thresholds decide)* |
| `Chunker:MinChunkChars` | `1` |
| `Chunker:MaxOverrunChars` | `200` |

Paths resolve against the **tool's project directory**, found by walking up from the build output to
`CoreRentalNet.CatalogIngestion.csproj`, so a run launched from `bin` reads and writes the same files as
one launched from the project.

## Store

One table in the tool's own SQLite file, written with raw `Microsoft.Data.Sqlite`:

```sql
CREATE TABLE IF NOT EXISTS product_embedding (
    id          TEXT NOT NULL PRIMARY KEY,
    skuNo       TEXT NOT NULL,
    name        TEXT NOT NULL,
    description TEXT NOT NULL,
    embedding   BLOB NOT NULL,
    created_at  TEXT NOT NULL
);
```

- `id` — a new GUID per row.
- `skuNo` — the catalogue's own key; **many rows share it**.
- `name`, `description` — the product's full values, repeated on every one of its rows.
- `embedding` — 384 little-endian floats, 1,536 bytes.
- `created_at` — the run's UTC time, ISO-8601, the same on every row of one run.

There is **no recipe table**: nothing records the model, width, chunker or catalogue hash.

## Guarantees

1. **Refuse before writing** — an empty catalogue, a product with no chunks, or a wrong-width vector
   leaves the existing table untouched.
2. **Replace, never append** — re-running is safe; the table always reflects one run of the current file.
3. **Deterministic text** — the renderer is machine-independent, and the chunker is pinned and configured.
4. **Configurable, never silent** — every value has a stated default and an unusable one is fatal.

## Failure modes

| Failure | Behaviour |
|---|---|
| Embedding server unreachable / model missing | run fails; nothing written |
| A vector not `Embedding:Width` wide | run fails naming the width; nothing written |
| A product yields zero chunks | run fails; nothing written |
| Empty catalogue | run fails; nothing written |
| A required value present but unusable | run fails naming the key; exit non-zero |

## Tests

All in `tests/CoreRentalNet.CatalogIngestion.Tests/` (38 tests), against hand-written stand-ins for the
server and the splitter — no network, and no claim about retrieval quality:

- **The chain** — `IngestionPipelineTests`: a real `products.json` and a real SQLite file; one row per chunk
  carrying its product's name and description; re-running replaces; a zero-chunk product or a wrong-width
  vector refuses the run and leaves the stored rows intact. The loader's refusal of an empty catalogue is
  asserted too, because it is what makes an empty table unreachable.
- **The store** — `SqliteProductEmbeddingStoreTests`: the table is created on first write, ids are unique,
  one timestamp per run, the blob decodes to the floats written, **ranking the stored rows by cosine finds
  the nearest**, and a database holding a **foreign table is refused rather than emptied** — the search
  asserted where a consumer would perform it, since the tool has no search method of its own.
- **The renderer** — `EmbeddedTextTests`: name, description and every metadata value; attributes ordered so
  the text is reproducible; an empty section omitted entirely.
- **The splitter** — `SemanticProductChunkerTests`: the real `SemanticChunker.NET` yields vectors of the
  configured width, the same way twice, and a short answer from the server is refused rather than paired by
  position.
- **The codec** — `VectorBlobTests`: four bytes per float, written little-endian. The tool only writes, so
  there is no decoder to test; a consumer's expectation is pinned as the exact bytes.
- **Configuration** — `ConfigurationReaderTests` (17): defaults, a present-but-unusable value refused by
  name, and paths resolved against a base directory it is given rather than one it finds itself.

## Baselines

Recorded at the end of this change:

| Baseline | Measured |
|---|---|
| `BUILD` | `dotnet build CoreRentalNet.sln` — **0 warnings, 0 errors** |
| `NONBROWSER` | **819 passed, 0 failed** across all ten offline projects |
| `BROWSER` | not run; unchanged by this work |
| `AGENT` | not run; unchanged by this work |
| Live run | **638 rows / 205 products** written in 25 s against the local server; schema, 1,536-byte blobs, GUID ids and one UTC timestamp verified with `sqlite3`; a direct nearest query returns desks and a monitor for "work with two screens" and coffee items for "relax with a coffee" |

`ARCHITECTURE` is one of the ten and passes, having caught and then confirmed the fix for **ARC-05** (no
project may pin its own package versions).
