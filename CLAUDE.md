# CoreRentalNet — agent instructions

Read this first. It is the auto-loaded entry point; the authority chain below wins over any
assumption in a prompt.

## Authority chain (read before structural work)

1. `CONVENTIONS.md` — the conventions: **persistence-record Domain + Application services**, the
   coding budgets, and the test rules. Each rule names the architecture test that enforces it.
2. `README.md` — the module map, the trust model and the SQLite constraints.
3. `specs/baselines.md` — what `BUILD`, `NONBROWSER`, `BROWSER` and `AGENT` mean, and where each is
   measured.
4. `specs/adr/` — the decisions, with the alternatives that were rejected and why.

The plan documents this chain once named — a release plan, epic capsules and a test matrix — were
removed deliberately and are not coming back, so `specs/baselines.md` and `specs/adr/` are the record
now. The chain is stated here and nowhere else, because it was once stated twice and the copies
drifted: three documents went on naming five files after `specs/` had stopped holding them.

## Hard rules

- **A step is done only when the baselines are green.** `specs/baselines.md` defines them —
  `BUILD`, `NONBROWSER`, `BROWSER`, `AGENT` — and where each is measured. Record the numbers where
  the work is described.
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
- **Do not save changes, artifacts or commits.** Nothing is written or committed without the product
  owner asking for it.
- **Do not push to origin.**
- **Do not take shortcuts.**
- **Do not assume.**
- **Do not do anything other than what was discussed.**
- **Be clear on the plan; no assumption.** State the plan plainly before acting.
- **Do not build when a requirement is unclear or an assumption is present — ask first.**

## Conventions

- .NET 10, nullable enabled, warnings as errors. No mocking library — hand-written fakes.
- Every public type crossing a layer boundary gets a comment saying *why*, not *what*.
- Tests: `[Fact]`/`[Theory]` with a matrix id where one exists.
- One revertible commit per step; update the architecture test and its non-vacuity guard in the same
  commit as the code it guards.
