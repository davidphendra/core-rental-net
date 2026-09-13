# ADR-0015: Pinned packages, and the FluentAssertions licence decision

- **Status:** Accepted (2026-09-10)
- **Decided by:** product owner

## Decision
- **Central package management with exact versions.** `Directory.Build.props`:
  `Nullable=enable`, `TreatWarningsAsErrors=true`, `AnalysisLevel=latest`,
  `EnforceCodeStyleInBuild=true`. `global.json` pins SDK `10.0.400`.
- **EF Core and `Microsoft.Data.Sqlite` pinned to exactly `10.0.12`.**
  This is a security fix, not hygiene: `Microsoft.EntityFrameworkCore.Sqlite`
  10.0.0 resolves `SQLitePCLRaw.lib.e_sqlite3` **2.1.11**, which carries a
  **high-severity** advisory with no fixed release in that branch. 10.0.12
  resolves 2.1.12.
- `SQLitePCLRaw.bundle_e_sqlite3` **2.1.12** forced via
  `CentralPackageTransitivePinningEnabled`.
- **Identity is Auth0's own package.** `Auth0.AspNetCore.Authentication` **1.11.0**
  (MIT, targets `net10.0`) is the integration point, so the scheme name, authority,
  logout endpoint and scopes are the provider's conventions rather than ours.
  `Microsoft.AspNetCore.Authentication.OpenIdConnect` **10.0.12** stays a direct
  reference rather than being left transitive: the wrapper is built on that
  handler, two session-policy settings (ADR-0019) are applied on it, and its exact
  version is pinned here rather than trusted to resolution.
- **`Microsoft.IdentityModel.JsonWebTokens` 8.22.0** is a direct reference for the
  same reason: an access token is read for its permissions (ADR-0019), so the token
  type it is read with is pinned here rather than left to whatever the handler
  resolves.
- **`AwesomeAssertions` 9.6.0** (Apache-2.0) instead of FluentAssertions 8.x,
  which ships a *non-commercial* Xceed licence.
- Tests: xUnit v2 (`2.9.3`), `Microsoft.NET.Test.Sdk` 17.14.1,
  `xunit.runner.visualstudio` 3.1.4, `Microsoft.Playwright` 1.62.0 bare with a
  hand-rolled host fixture, `NetArchTest.Rules` 1.3.2,
  `Microsoft.Extensions.TimeProvider.Testing` 10.10.0 for `FakeTimeProvider`.
- `dotnet-ef` pinned as a **local** tool at `10.0.12` via
  `.config/dotnet-tools.json`.
- Logging: built-in `Microsoft.Extensions.Logging` with the JSON console
  formatter. No Serilog.
- Deliberately absent: MediatR, AutoMapper, FluentValidation, Moq/NSubstitute,
  Testcontainers, EF InMemory, `Microsoft.AspNetCore.Mvc.Testing` (its in-memory
  `TestServer` is precisely the non-real server the E2E rule excludes),
  Swashbuckle/OpenAPI (there is no HTTP API), and a strongly-typed-id generator.
