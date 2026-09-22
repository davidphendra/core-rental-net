# 0002 — The ingestion tool's internals: internal by default, and what the review changed

**Status:** accepted
**Date:** 2026-09-21
**Deciders:** product owner, engineer

## Context

[0001](0001-catalog-ingestion.md) fixed what the tool **is**: standalone, local, one chunked table in a file
of its own. It says nothing about the inside of the tool. This record covers the pass that produced it, which
happened in two stages:

- **The code was restructured for readability** — the run split into named steps, and the entry point reduced
  to composition and exit codes.
- **The result was then reviewed** against SOLID, the coding standard in `CONVENTIONS.md`, C# conventions,
  nullability and security. **Eighteen findings** were raised. **Sixteen were applied** — they are the fifteen
  decisions below, S1 and S2 having landed together — and two stay open, recorded under Consequences and in the
  traceability table in Notes.

Constraints that shape the record:

- `CONVENTIONS.md`: one type per file; ≤ 150 lines per file, ≤ 40 lines per method, ≤ 3 nesting levels;
  **one public operation per service**; no mutable static state; nullable enabled with warnings as errors;
  no mocking library — hand-written fakes.
- **ARC-05**: a project may not pin its own package versions.
- The tool is an executable with **exactly one consumer: its own test project.** Nothing else in the solution
  references it.

## Decision

**D1 — All sixteen of the tool's types are `internal`, and the test project is granted access by name.**
`CoreRentalNet.CatalogIngestion.Tests` is listed in `InternalsVisibleTo`. Nothing references the tool, so it has
no surface to publish, and a `public` member would be a promise to a caller who does not exist.

**D2 — Every collaborator is a port with one operation, and the run is built from the ports.**
`IProductChunker`, `IEmbeddingClient` and `IProductEmbeddingStore` each declare exactly one operation, and
`IngestionPipeline` is constructed from those alongside the catalogue's own port. This is `CONVENTIONS.md`'s
"callers depend on the interface, never the class" and its "one public operation per service", and it is what
lets the whole chain be exercised with hand-written stand-ins and no network.

**D3 — Composition happens in one local function, which reads the settings it composed.**
`Run()` in the entry point builds the loader, the client, the chunker and the store, and closes over the
`IngestionSettings` from the enclosing scope rather than being handed them again. It is the only code that
composes the tool, so a parameter would be a second route for the same six values.

**D4 — The filesystem walk is its own type, so the reader stays a pure function.**
`ProjectDirectory.Resolve` walks up to `CoreRentalNet.CatalogIngestion.csproj`, with the base directory as the
honest fallback for a published copy that has no project file above it. `ConfigurationReader` then resolves
relative paths against **a directory it is given**, which makes it a function of its inputs and lets a test hand
it any directory it likes.

**D5 — The run needs the catalogue's port and a width, not a file path and a settings record.**
`IngestionPipeline` takes `IProductCatalog`, the chunker, the store, `int width` and a `TimeProvider`. Reading
and parsing `products.json` stays the catalogue module's business, and the run is handed the one configured
number it actually checks rather than six it does not use.

**D6 — The embedding client is handed its transport.**
`OpenAiCompatibleEmbeddingClient` takes an `EmbeddingClient`; the `OpenAI` client, its endpoint and its
placeholder key are assembled in `Build`. The type that talks to the network can therefore be driven through a
stand-in transport instead of only through a live server.

**D7 — A configured value is repeated in a refusal only if its key is on a list of repeatable keys.**
`ConfigurationReader` keeps `s_safeToRepeat` and one formatter, `Stated(key, value)`; every message that names a
value goes through it, and the list denies by default. A key added later — a token, a connection string —
cannot reach an operator's error line by being forgotten.

**D8 — The store refuses a file whose tables are not its own, and its table's name lives in one constant.**
`RefuseAForeignFile` reads `sqlite_master` before any write and throws naming the table it found; `TableName` is
shared by the schema, the delete and the check. The store empties its table on every run, so a `Database:Path`
aimed at an unrelated SQLite file would otherwise clear that file's data — this turns silent loss into a refusal
that says what it saw.

**D9 — The blob is written and never read.**
`VectorBlob` has `ToBytes` and no decoder, because the tool never reads its table and a decoder would be code no
run can reach. The format is a stated contract instead — **384 little-endian IEEE-754 floats, 1,536 bytes** —
and the tool's tests assert the exact bytes a consumer must expect.

**D10 — The tool's static tables are immutable.**
`s_thresholdTypes` is an `ImmutableArray<string>` and `s_safeToRepeat` an `ImmutableHashSet<string>`.
`CONVENTIONS.md` forbids mutable static state; this makes a read allocate nothing and leaves no caller able to
alter either.

**D11 — The empty-catalogue check stays, and says why.**
The catalogue loader refuses a file holding no products, so the branch cannot be reached today. It is kept, with
that written beside it, because writing an empty table would replace a good one with nothing, and a future
catalogue source is not obliged to refuse an empty file.

**D12 — A value that may be absent is a nullable, bound to a name.**
`Chunker:TargetChunkCount` is the one key whose absence is meaningful — unset means "let the thresholds decide" —
so it is read as `int?` and is the only key the reader does not default. The same shape appears where a refusal
must report what it found: `float[]? miswidthed` is the first vector of the wrong length, so the message can name
the width that arrived.

**D13 — Failure is reported by kind, and the exit code says which kind.**
`Describe` prints the exception's message for the failures the tool expects — `HttpRequestException`,
`IOException`, `UnauthorizedAccessException`, `ClientResultException`, `ProductLoadException` — and the whole
`ToString()` for anything else, because for a defect the stack is what says where it happened. The exits are
distinct: **0** for a successful run or `--help`, **2** for an unknown argument or an unusable configuration,
**1** for a run that failed.

**D14 — The help text is a type of its own.**
`Usage.Text` is not a member of `IngestionSettings`: it is what the tool says about itself, and not part of what
a run is configured with.

**D15 — The key the local server ignores is a named constant.**
`PlaceholderApiKey = "local"`. The OpenAI client will not be constructed without a key and this server checks
none, so naming it is what tells a reader it is a placeholder rather than a credential that reached the source;
a server that checks a specific key would need a real one, which would be a new setting and not this constant.

## Consequences

**Good**

- The whole chain — catalogue, chunk, vector, row — is exercised offline against stand-ins and a real SQLite
  file, so the suite makes a claim about behaviour rather than about a server being up.
- Everything that could destroy data or store an incomparable vector is refused before the first write, and each
  refusal names the key, the file, the product or the width.
- No type in the tool is reachable from outside it, so its internals can change without another project noticing.
- Measured: **0 warnings, 0 errors** on `dotnet build CoreRentalNet.sln`, and **819 passed, 0 failed** across the
  ten offline projects.

**Bad**

- **The transport seam is built and unexercised.** `Build` exists so the client can be driven through a stand-in
  transport, and the tool's tests stand in for `IEmbeddingClient` but never for the `EmbeddingClient` beneath it
  — so the OpenAI request shape this tool actually sends is unproven.
- **Two findings stay open**: `ConfigurationReader.Text`'s nested conditional, and `Console.CancelKeyPress` not
  being wired to a `CancellationTokenSource`, so Ctrl-C is not a clean cancellation.
- **Nothing records what built a vector.** [0001](0001-catalog-ingestion.md) already carries this, and this record
  does not change it: the inside of the tool is now documented, and the run's inputs still are not stored.

## Alternatives rejected

| Alternative | Why rejected |
|---|---|
| Leave the tool's types `public` | Nothing references the tool, so a public surface would be a promise to a caller who does not exist, and every internal change would become a compatibility question |
| One concrete class for the whole run | With no port to substitute, the chain could only be exercised against a live embedding server and a real catalogue file |
| Thread the settings into `Run()` as a parameter | It is the only code that composes the tool; a parameter would be a second route for the same six values, and the two could drift |
| Let `ConfigurationReader` find the project directory itself | Finding it is a filesystem walk, which would make the reader impure and untestable without the real filesystem |
| Hand the run the settings record, and a path to the catalogue | The run would depend on six values to use one, and would parse `products.json` itself — work the catalogue module already owns and tests |
| Construct the `OpenAI` client inside `OpenAiCompatibleEmbeddingClient` | The tool's only network seam would be unreachable without a server running |
| Echo every configured value in a refusal, as the earlier code did | A secret added to the settings later would appear in an operator's log line the moment anyone mistyped it |
| Empty whatever file `Database:Path` names | A path pointed at an unrelated SQLite file would clear that file's data, and nothing would report it |
| Keep a `ToFloats` decoder beside `ToBytes` for symmetry | Unreachable code, and a second place for the byte format to drift from the first |
| A mutable `string[]` and `HashSet<string>` for the two static tables | A caller could alter them, and every read would allocate; `CONVENTIONS.md` forbids mutable static state |
| Remove the empty-catalogue check, since the loader already refuses one | It would make the tool's safety a property of the loader rather than of the tool, and a different catalogue source could reach the write |
| A sentinel for "unset" — a zero count, an empty vector | A sentinel is a value the type calls valid, so every reader has to remember the exception; a nullable makes the absence the type's own business |
| One non-zero exit code, and the message printed for every failure | An operator could not tell a typo in a settings file from a server that is down, and a defect would print one line with no location |
| Keep the help text on `IngestionSettings` | It is what the tool says about itself, not part of what a run is configured with |
| An inline literal for the placeholder key, or a new setting for it | A bare literal in the source reads like a credential that leaked, and a setting the local server ignores is a knob that does nothing |

## Notes

- **Traceability.** The review's findings were grouped S/C/T/X/Y. This is where each landed.

  | Finding | Landed as |
  |---|---|
  | S1, S2 | **D5** — the run takes `IProductCatalog` and `int width` |
  | S3 | **D4** — `ProjectDirectory`, and a reader given its base directory |
  | S4 | **D14** — `Usage` |
  | S5 | **D6** — the transport is handed in, `Build` constructs it |
  | C1 | **D11** — the empty-catalogue check, documented as defence in depth |
  | C2 | **D1** — `internal` by default, with `InternalsVisibleTo` |
  | T1 | **D13** — `Describe` |
  | T2 | **open** — `Console.CancelKeyPress` is not wired to cancellation |
  | T3 | **D10** — immutable static tables |
  | T4 | **D9** — no decoder |
  | X1 | **D15** — the placeholder key |
  | X2 | **D7** — echoing is allow-listed, so it denies by default |
  | X3 | **D8** — the foreign-file refusal, and one table-name constant |
  | Y1 | `System.*` usings first and outside the namespace — a convention, applied across the tool |
  | Y2 | **D3** — `Run()` reads the settings it composed |
  | Y3 | **D12** — `float[]? miswidthed` |
  | Y4 | **open** — `ConfigurationReader.Text`'s nested conditional |

- **The readability restructure is not a decision and is not recorded as one.** It is the shape D1–D15 sit
  inside: `IngestionPipeline` was split into `RunAsync`, `RefuseUnusable` and the row it builds, and the entry
  point into `Run`, `Describe` and `Configuration`.
- **The tool's release was three Conventional Commits** — the plan documents, then Discovery's side of the
  change, then the tool — fast-forwarded into `main` with no merge commit, matching a history that has none.
  Nothing was pushed: the repository has no remote.
- **D6, D9 and D15 were later superseded by [0003](0003-discovery-reads-the-tool.md).** The embedding client
  and the blob codec moved into the shared kernel so the application could use the same ones; the tool's blob
  therefore still writes and never reads, but the decoder beside it exists and belongs to the reader. Nothing
  else in this record changed.
- **`SemanticChunker.NET` brings `ICU4N`**, whose target raises `ICU4N_IDE_0002`. It is demoted in the tool's own
  `.csproj` and in its test project's because the target travels to consumers. [0001](0001-catalog-ingestion.md)
  records the same fact; it is the one alternative here that was rejected as a repository-wide change.
