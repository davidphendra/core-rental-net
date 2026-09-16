# CoreRentalNet — agent instructions

Read this first. It is the auto-loaded entry point; the authority chain below wins over any
assumption in a prompt.

## Authority chain (read before structural work)

1. `specs/tech-architecture/architecture-style.md` — **persistence-record Domain + Application
   services**. Type placement, coding standards, and the invariant relocation map.
2. `specs/REFACTOR_DDD_TO_CLEAN_LATEST.md` — the migration complexity matrix and the staged plan.
   Execute its stages in order; stage 0 (behaviour lock) is mandatory.
3. `specs/architecture.md` — module map, trust model, measured SQLite facts.
4. `specs/test-matrix.md` — 101 scenarios; the requirement itself.
5. `specs/state.yaml` — the live phase and next action.

## Hard rules

- **The requirement does not change.** A structural step is done only when all three baselines are
  green: `BUILD`, `NONBROWSER`, `BROWSER` (see `architecture-style.md` §7). Record the numbers in
  `specs/verifications/` at the start and end of every stage.
- **Domain is records. Application is services.** A persisted type is a plain record with no
  business methods. Business rules live in an `I…Service` implementation in `Application`.
- **Callers depend on the interface, never the class.** DI injects the `sealed` implementation
  (SOLID dependency inversion,/0025).
- **One type per file.** File name equals the type name; no second class, record, interface, enum or
  struct in the same file. Request/response records and interfaces each get their own file.
- **Small classes, KISS.** ≤ 150 lines of code per file (documentation and comments excluded),
  ≤ 40 lines per method, ≤ 3 nesting levels, one public operation per service. No speculative
  abstraction. Enforced by `ArchitectureTests/CodingStandardTests`.
- **Microsoft C# conventions.** PascalCase types/public members; camelCase locals/parameters;
  `_camelCase` private instance fields; `s_` private static fields; PascalCase constants; `I…`
  interfaces; file-scoped namespaces; usings outside the namespace with `System.*` first; `var` only
  when the type is obvious. References in .
- **No mutable static state.** `const` and immutable `static readonly` are allowed; keep working
  values in method locals, not fields. Pure stateless static helpers are permitted.
- **The Dependency Rule still holds.** No EF Core, ASP.NET or SQLite type in Domain (ARC-04). No
  module reaches another module's `Domain` or `Infrastructure` (ARC-01).
- **Concurrency is manual now.** Every write service must bump the record's `Version`; a forgotten
  bump is a silent lost update.
- **Never deploy or provision without explicit product-owner consent**.
- **No secrets in the repository.** `appsettings.Local.json` is gitignored and development-only.

## Conventions

- .NET 10, nullable enabled, warnings as errors. No mocking library — hand-written fakes.
- Every public type crossing a layer boundary gets a comment saying *why*, not *what*.
- Tests: `[Fact]`/`[Theory]` with a matrix id where one exists.
- One revertible commit per step; update the architecture test and its non-vacuity guard in the same
  commit as the code it guards.
