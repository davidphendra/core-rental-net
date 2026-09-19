# CONVENTIONS.md

The code and test conventions this repository enforces. `CLAUDE.md` is the agent entry point and
carries the authority chain; this file is the convention list those rules refer to.

## Authority chain

The chain lives in `CLAUDE.md`, which is the entry point and names the documents in the order they
are read.

It is deliberately **not repeated here**. It was, and the two copies drifted: this file and `CLAUDE.md`
both went on naming five files after `specs/` had stopped holding them, and the note that recorded the
gap pointed at a sixth file that was gone as well. One list, in one place.

## Platform

- .NET 10, `LangVersion latest`, nullable enabled, warnings as errors, analyzers on.
- No mocking library. Hand-written fakes prove the port is small enough to fake.
- Central package management (`Directory.Packages.props`).

## Code

- **Domain is records. Application is services.** A persisted type is a plain record with no business
  methods. Business rules live in an `I…Service` implementation in `Application`.
- **Callers depend on the interface, never the class.** DI injects the `sealed` implementation
  (SOLID dependency inversion).
- **One type per file.** File name equals the type name; no second class, record, interface, enum or
  struct in the same file. Request/response records and interfaces each get their own file.
  Enforced by `ArchitectureTests/OneTypePerFileTests`.
- **Small classes, KISS.** ≤ 150 lines of code per file (documentation and comments excluded),
  ≤ 40 lines per method, ≤ 3 nesting levels, one public operation per service. No speculative
  abstraction. Enforced by `ArchitectureTests/CodingStandardTests`.
- **Microsoft C# conventions.** PascalCase types/public members; camelCase locals/parameters;
  `_camelCase` private instance fields; `s_` private static fields; PascalCase constants; `I…`
  interfaces; file-scoped namespaces; `using` directives outside the namespace with `System.*` first;
  `var` only when the type is obvious.
- **No mutable static state.** `const` and immutable `static readonly` are allowed; working values
  live in method locals. Pure stateless static helpers are permitted.
- **The Dependency Rule.** No EF Core, ASP.NET or SQLite type in Domain (ARC-04) or Application. No
  module reaches another module's `Domain` or `Infrastructure` (ARC-01).
- **Concurrency is manual.** Every write service bumps the record's `Version`; a forgotten bump is a
  silent lost update.
- **Boundary comments.** Every public type crossing a layer boundary gets a comment saying *why*, not
  *what*.

## Tests

- `[Fact]`/`[Theory]` with a matrix id where one exists.
- F.I.R.S.T.: fast, isolated, repeatable, self-checking, timely.
- Name a test after the behaviour it proves.
- Every new function has at least one test; every bug fix has a regression test.
- Tests verify behaviour through public interfaces, not implementation details.
- Hand-written fakes live beside the tests that use them.

## Specs and process

- Output documents are written under `specs/`.
- One revertible commit per step; update the architecture test and its non-vacuity guard in the same
  commit as the code it guards.
- Never deploy or provision without explicit product-owner consent.
- No secrets in the repository. `appsettings.Local.json` is gitignored and development-only.
- `gh` is used only for pull requests and repository clones; never `gh issue create`, never the
  GitHub REST API directly.
