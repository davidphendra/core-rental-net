# ADR 0025 — The tag is the release identity, and the pipeline stamps it

**Status:** Accepted
**Date:** 2026-10-05
**Related:** `specs/baselines.md`

## Context

Two deployables ship from this repository: the Blazor application (`src/Host`) and the Foundry
hosted agents (`agentfoundry/`). Neither had one answer to "which build is this?". The application
pipeline passed `$(Build.BuildNumber)` — a date, such as `20251005.1` — as the version. The footer
refuses anything that is not a semantic version, so the deployed application silently displayed the
floor `0.0.1` instead of the build it was running. The agent pipeline passed no version at all, and
the agent's managed code is built remotely, so it carried whatever the SDK defaulted to.

A version that no one can read from the running artifact is not a version. This decision makes the
Git tag the release identity, the pipeline the place it is resolved, and the assembly properties the
way it reaches the binary and the runtime.

## Decision

1. **A release is an annotated `vX.Y.Z` tag.** The pipeline resolves the version from the tag it is
   building; a build with no tag keeps a floor. The build itself never reads Git — the same practice
   the .NET platform repositories use — so a local build and a pipeline build differ only in the
   value the pipeline supplies.
2. **One resolver, both pipelines.** `scripts/resolve-version.sh` strips `refs/tags/v`, refuses a
   value that is not a semantic version, falls back to the floor, takes the short commit, and emits
   `version`, `informationalVersion` and `shortSha`. Both pipelines call it, so the two deployables
   cannot disagree about the release.
3. **The version properties are computed once, in `Directory.Build.props`.** `AssemblyVersion` is the
   stabilized `MAJOR.MINOR.0.0`; `FileVersion` is the release `MAJOR.MINOR.PATCH.0`; the
   informational version is the release plus the commit as build metadata (`1.4.1+abc1234`). A
   pre-release label is part of the informational version and is dropped from the numeric ones, which
   cannot carry one.
4. **Build metadata identifies the artifact; SemVer identifies the release.** The commit lives after
   `+` and is never part of the version the footer shows.
5. **The agent is a special case, because it is built remotely.** `azure.yaml` sets
   `codeConfiguration.dependencyResolution: remote_build`, and `CoreRentalNet.Agents.csproj` records
   that Foundry zips only the project folder. The repository root's `Directory.Build.props` is
   therefore not in that zip, so the pipeline writes a generated `Version.g.props` **inside the
   project folder** before `azd deploy`; the csproj imports it when present and carries its own copy
   of the mapping. The agent's version reader is duplicated inside the project folder for the same
   reason a linked source file would not travel in the zip.
6. **Runtime exposure is read-only and anonymous.** The application publishes
   `GET /api/version` returning `{version, build, environment}`; the agent prints the same pair in
   its startup diagnostics. Neither exposes configuration.
7. **Artifact identity is immutable per release.** The application's pipeline artifact is named with
   the version and the short commit, and the build ID is the identity; the agent's identity is the
   Foundry agent version the deploy registers. A given SemVer is never rebuilt from a different
   commit.
8. **One version authority per deployment.** The Azure DevOps pipeline owns the version and the
   artifact; the GitHub Actions workflow stays a non-authoritative browser-suite gate.

## Alternatives rejected

- **`$(Build.BuildNumber)` as the version.** The originating defect: not a semantic version, and
  refused by the display rule.
- **Nerdbank.GitVersioning or GitVersion.** Automatic pre-release numbering from branch position is
  real value, but these reverse the deliberate "the build does not read Git" decision, add a tool and
  a `version.json`, make local builds depend on local Git history, and would rewrite the tested
  informational-version rule. Adopt one only if automatic branch-derived pre-release versions become
  a requirement.
- **Hardcoding `<Version>` in a `.csproj`.** The thing this decision exists to avoid: the project
  file stops being true the moment the next tag is cut.
- **Passing build properties to `azd` remotely.** Would be the cleanest agent mechanism, but azd's
  support for build arguments on `remote_build` is not established. Not assumed.
- **A container image and registry digest.** The strongest immutability, and the wrong size for a
  demonstration that already deploys a zip to App Service. Revisit if the deploy target changes.
- **An authenticated `/api/version`.** Security theatre: the footer already shows the version to
  every anonymous visitor, and the endpoint exists to pin a report to an artifact without
  credentials. Only the version, the short commit and the environment name are exposed.

## Consequences

- The application footer and `GET /api/version` now show the release the pipeline stamped, ending the
  silent fallback.
- Changing `Directory.Build.props` changes the agent's CI build too; the agent pipeline already
  triggers on that path, and `AGENT` is part of the baseline set for exactly this reason.
- A new release is a tag. Cutting one without a tag leaves the floor in place rather than a guessed
  version.
- The agent's stamp is only as correct as the pipeline step that writes `Version.g.props`; the
  `AGENT-DEPLOY` smoke exists to prove it reached the remotely built artifact.
