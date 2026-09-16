# audit-code — epic e01 (catalogue API)

Scope: branch `catalog-api`, `main..HEAD` (`acc9733..0305d81`), 36 changed files — 10 source files,
10 test files, 2 packages, and the plan/verification documents.

Method: the checklist below, applied by hand to the diff. Scripts this repository does not ship are
named where they would have run.

Verdict: **all items pass.** Two LOW smells were considered and kept, with the reason recorded.

## Checklist

### Supply Chain & Security
- ✓ New packages tagged in plan-work: `Microsoft.AspNetCore.Mvc.Testing` `[OK]`,
  `Microsoft.AspNetCore.Authentication.JwtBearer` `[OK]` — both Microsoft, both pinned exactly.
- ✓ No `[SLOP]`/`[SUS]` package. No unmocked dependency added.
- ✓ No secrets in the diff (`sk-`, `ghp_`, `AKIA`, `.env`, client secrets): scanned, none.
- ✓ OWASP spot-check: no injection surface (in-memory `string.Contains`, no SQL/shell/path/reflection);
  authorization reuses the existing requirement and names only the bearer scheme; no sensitive data
  in the response beyond the public catalogue; the security headers apply to API responses.
- ✓ No unaddressed HIGH finding — `specs/security/REVIEW.md` (4 LOW/INFO, all accepted in scope).
  (`security-threats.md` referenced by the skill does not ship here.)

### Provenance & Metadata
- ✓ Plan artefacts carry `type:`/`context:` metadata (story specs) and the epic has `bcps`/`risk`.
- ✓ Steps reference the decisions and the verification records; commits are Conventional.

### Law of Demeter
- ✓ No chains through unrelated objects. `context.User.FindFirst("sub")?.Value` and
  `loggers.CreateLogger(...)` are framework calls on immediate collaborators.

### CONVENTIONS.md Compliance
- ✓ All output documents under `specs/`.
- ✓ No `gh issue create`; no direct GitHub REST calls; `gh` untouched.

### Scope
- ✓ Changes limited to the epic: 10 new source types, 10 test types, 2 packages, documents. Nothing
  refactored or reorganised outside the catalogue API.
- ✓ No speculative feature: no pagination, envelope, OpenAPI, MCP, rate limiting, or broader search.
- ✓ No discovered-and-ignored gate failure: the one failure found (the non-hermetic test suite) was
  fixed in the story that exposed it.

### Boy Scout Rule
- ✓ Touched files are cleaner than found; `CatalogApiFactory`'s false comment about Development was
  corrected when the environment changed.
- ✓ No dead code, no commented-out blocks (scanned), no `TODO`/`HACK`/`Console.Write`.

### Types and Safety
- ✓ C# with nullable enabled; no untyped public member. No suppression attributes added.
- ✓ No `dynamic`/reflection introduced.

### Test Coverage
- ✓ Every new behaviour has a test: binding + refusal (12), endpoint over HTTP (8), policy wiring (3),
  authorization over HTTP (4), logging (4). Matrix rows API-01..API-12 all covered.
- ✓ `CatalogApiBinding` and `CatalogController` are not named in a test file **by design**: their
  behaviour is asserted through `Bind` and through real HTTP requests — testing them directly would be
  testing implementation detail, which CONVENTIONS forbids.
- ✓ F.I.R.S.T.: fast (ms), isolated (a temp database per factory), repeatable (the `Testing`
  environment makes the suite independent of the developer's machine), self-checking, timely.

### SOLID and Heuristics
- ✓ Single responsibility: binding, result, mapping, logging, scheme, JSON, policy name are seven
  separate concerns in seven types.
- ✓ Open/Closed: the API reuses `CatalogReadRequirement`/`CatalogReadAuthorizationHandler` without
  modifying either; the second policy name is the only extension.
- ✓ Dependency inversion: the endpoint injects `ISearchCatalogHandler`.
- ✓ Chapter 17: no comments restating code, no magic numbers, no inconsistent naming. Behaviour-named
  tests.

### Refactoring Smells (Fowler)
- ✓ No Mysterious Name, Feature Envy, Data Clumps, Message Chains, or Middle Man.
- Duplicated Code: **considered, kept** (A2 below).
- Primitive Obsession: the three raw query strings are the HTTP boundary's own shape and are
  converted to a `SearchCatalogQuery` immediately; `CatalogApiParameters` is that boundary type.

### Code Style
- ✓ Functions well under 20 lines; files 14–109 lines (limit 300 code lines).
- ✓ Names specific; `Catalog*` names appear only where the catalogue is meant.
- ✓ Early returns over nesting; `Bind` uses guard clauses.
- ✓ Comments explain why (the hermeticity, the scheme, the ordering after authorisation).

### Red Flags (rationalizations I caught)
- I was tempted to skip the security section because the endpoint is read-only; the skill's hard gate
  says not to. I ran it and wrote `specs/security/REVIEW.md`.
- I was tempted to call the two unexercised scripts a pass. They are recorded as **not run**.
- I considered quietly dropping `CatalogApiBinding`'s file because it is nearly an empty record; it is
  a result type the binding returns, not a middle man, so it stays.

## Findings kept

| ID | Severity | Finding | Reason kept |
|---|---|---|---|
| A1 | LOW | `CatalogApiLog.Category` is a string literal mirroring the type's full name; `typeof(CatalogApiLog).FullName` would remove the drift risk if the type moved. | The literal and every assertion use the same constant, so no test can drift; renaming the type would be a deliberate act. Keeping it avoids editing verified code for a cosmetic gain. |
| A2 | LOW | `CatalogApiFactory` and `CatalogApiAuthorizedFactory` repeat ~10 lines of temp-database configuration and disposal. | Both factories read on their own, the duplication is confined to tests, and a shared base would obscure why each exists. |

## Not available in this repository

`scripts/bp-churn-rank.sh`, the supply-chain `slopcheck` script, `scripts/check-blind-spots.sh`,
`scripts/lib/completeness-critic.sh`, `docs/references/security-threats.md`, and the skill's own
verify (`skills/enforce-first`, `skills/request-review` directories). Recorded as **not run** rather
than passed.
