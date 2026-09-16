# Modular monolith audit — CoreRentalNet

Skill: `audit-modular-monolith`, rule catalog
[`REFERENCE.md`](../../../.pi/agent/skills/audit-modular-monolith/REFERENCE.md). Date: 2026-09-16.
Scope: the four declared modules — **Catalog**, **Rentals**, **Workspace**, **BuildingBlocks**.

## Declaration and mechanical result

The module map is `specs/modules.map`. The map declares four modules and three allowed edges.

```
allow catalog   -> buildingblocks
allow workspace -> catalog, buildingblocks
allow rentals   -> catalog, workspace, buildingblocks
```

| Check | Command | Result |
|---|---|---|
| Module-map self-test | `check-module-boundaries.py --self-test` | self-test OK |
| Declared boundaries | `check-module-boundaries.py --map specs/modules.map --root .` | **clean**, 0 flags, 0 warnings |

The mechanical checker is clean, so every finding below came from reading the code against the
rule catalog, not from a checker hit. No HIGH finding exists. The findings are MEDIUM and LOW, and
per the skill they are recorded rather than fixed.

## Findings

| ID | Severity | Module or File:line | Rule | Source | Fix |
|---|---|---|---|---|---|
| M1 | MEDIUM | `Workspace.Application/Queries/Views/WorkspaceView.cs:6`, `AssignableSlot.cs:9`, `QuoteLine.cs:9` | The published contract must be minimal and stable; data is reached through the module's published contract | [REFERENCE.md §3 measure 2, §6 rung 2](../../../.pi/agent/skills/audit-modular-monolith/REFERENCE.md), [Spring Modulith named interfaces](https://docs.spring.io/spring-modulith/reference/fundamentals.html) | Publish the slot/state vocabulary in the contract namespace, or move it to `BuildingBlocks` if it is genuinely shared. Deferred: the enum is used across three modules' layers. |
| M2 | LOW | `src/BuildingBlocks` (whole) | A shared "common" kernel must have an owner and must not become the place every module dumps code | [REFERENCE.md §7 stop conditions, §11.2](../../../.pi/agent/skills/audit-modular-monolith/REFERENCE.md) | Watch item. The kernel has a stated charter and its own unit + integration tests; it is not drifting today. |
| M3 | LOW | `Workspace.Infrastructure/Configuration/DraftEntityTypeConfiguration.cs:13,35`; `Rentals.Infrastructure/Configuration/*:12–52` | Data isolation ladder — Level 1 (separate tables, one database) | [REFERENCE.md §5 data isolation ladder](../../../.pi/agent/skills/audit-modular-monolith/REFERENCE.md) | Correct for the current size. Extraction would need Level 2 (schema per module) or Level 3 first. No demand today. |
| M4 | LOW | `.github/workflows/ci.yml`; `specs/modules.map` | Enforcement ladder — a machine must check the boundaries in CI at the earliest rung that can express the rule | [REFERENCE.md §6 enforcement](../../../.pi/agent/skills/audit-modular-monolith/REFERENCE.md) | The call-graph check exists as a script but is not wired into CI, and CI is dormant. Wire the script when the repository gets a remote. |
| M5 | LOW | `specs/modules.map` line for `rentals` | The declared `api=` must name the namespace other modules are allowed to reach | [REFERENCE.md §6 Spring Modulith `verify()`](../../../.pi/agent/skills/audit-modular-monolith/REFERENCE.md) | The map named `Rentals.Application.Contracts`, which is not a namespace in the source. The published surface is `Rentals.Application`. **Fixed: the map now declares `api=CoreRentalNet.Modules.Rentals.Application`.** |

### M1 — the read contract is not self-contained

`WorkspaceView` names `DraftState`, and `AssignableSlot` and `QuoteLine` name `SlotId`. Both are
`CoreRentalNet.Modules.Workspace.Domain` types. A consumer that receives the view must therefore
also reach the module's Domain to interpret it — which is exactly what the Host does:

```
src/Host/CoreRentalNet.Host/Presentation/IWorkspaceSession.cs:2   using CoreRentalNet.Modules.Workspace.Domain;
src/Host/CoreRentalNet.Host/Presentation/SlotCatalog.cs:4         using CoreRentalNet.Modules.Workspace.Domain;
src/Host/CoreRentalNet.Host/Presentation/SlotCss.cs:1             using CoreRentalNet.Modules.Workspace.Domain;
src/Host/CoreRentalNet.Host/Presentation/ProductGlyph.cs:2        using CoreRentalNet.Modules.Workspace.Domain;
```

The Host is the composition root and the presentation, not a fifth business module, so it is
outside the compile-time rule that `ModuleBoundaryTests` enforces. The finding is still real: the
module's **published** read shape is not self-contained, and a second consumer would inherit the
same reach. This protects the extraction path, not correctness, so it is MEDIUM.

The counter-argument, recorded honestly: the `Host` must map seven slots to glyphs and CSS classes,
and the slot vocabulary is a genuine shared concept between the module and its only consumer. A
contract-level copy of the enum would duplicate it. The clean fix is to treat the slot vocabulary as
shared and publish it from `BuildingBlocks`, which is a larger change than this audit should make.

## What is already correct

**Deployment and modularity axes.** One deployable unit (the Host), four logical modules with
enforced boundaries: compiler (separate assemblies, `internal` resolver), then architecture tests
(`ModuleBoundaryTests`).

**Data ownership (rule group A).** One module per table, and the table names carry the owner:
`Workspace_Draft`, `Workspace_SlotAssignment`, `Rentals_Rental`, `Rentals_RentalLine`,
`Rentals_Invoice`, `Rentals_InvoiceLine`, `Rentals_NumberSequence`. No table is written by two
modules, there is no cross-module foreign key, and there is no shared ORM entity. Catalog owns no
tables: it reads a JSON file.

**Contracts (rule group B).** `Workspace.Application` reaches Catalog only through
`CoreRentalNet.Modules.Catalog.Application.Contracts`; `Rentals.Application` reaches Workspace only
through `CoreRentalNet.Modules.Workspace.Application.Contracts.Conversion`, and Catalog through its
contracts. Nothing reaches another module's `Domain` or `Infrastructure`.

**Dependencies (rule group C).** The declared graph is acyclic: `catalog -> buildingblocks`,
`workspace -> catalog, buildingblocks`, `rentals -> catalog, workspace, buildingblocks`. The
checker's `CYCLE` rule reports none.

**Integration (rule group D).** Every cross-module edge is a **direct call** through an interface —
the right style for a system that needs strong consistency in-process and has no extraction
roadmap (REFERENCE.md §4). Edges: `Workspace -> Catalog` through `IProductCatalog.Find`;
`Rentals -> Catalog` through `IProductCatalog.Find`; `Rentals -> Workspace` through
`IConvertWorkspaceToOrder`. No messaging, no outbox, no shared database reads.

**Slicing (rule group E).** Each module is a business capability, not a technical layer: Catalog is
"what can be rented", Workspace is "what the customer is assembling", Rentals is "what was rented
and what it costs". `BuildingBlocks` is the shared technical kernel by design, not a business layer.

**Enforcement (rule group F).** Rung 2 is in place and non-vacuous:

| Rule | Test |
|---|---|
| A module reaches another only through Application contracts | `ModuleBoundaryTests.No_module_depends_on_another_modules_domain_or_infrastructure` (ARC-01) |
| Domain holds no EF Core, ASP.NET or SQLite type | `ModuleBoundaryTests.Domain_assemblies_do_not_depend_on_ef_core_or_asp_net` (ARC-04) |
| Catalog publishes its vocabulary, not its Domain types | `ModuleBoundaryTests.The_catalog_port_publishes_its_vocabulary_and_not_the_domain_types` |
| Coding standard per repository rule | `CodingStandardTests`, `OneTypePerFileTests` |
| The boundary rule cannot go vacuous | each NetArchTest asserts the module count it discovered |

**Extraction readiness (rule group G).** No extraction is planned. The mechanism supports one:
all cross-module traffic goes through interfaces, so lifting a module into its own process would not
change any contract. The blocker would be the shared SQLite file — Level 1 on the ladder.

## Data isolation and integration style per edge

| Edge | Data isolation | Integration style | Consistency |
|---|---|---|---|
| `workspace -> catalog` | Catalog owns a JSON file, Workspace owns `Workspace_*` tables | direct call (`IProductCatalog`) | strong, in-process |
| `rentals -> catalog` | as above | direct call (`IProductCatalog`) | strong, in-process |
| `rentals -> workspace` | separate tables in one SQLite file (Level 1) | direct call (`IConvertWorkspaceToOrder`) | strong, same transaction boundary per handler |
| `* -> buildingblocks` | none (no owned business data) | direct call | strong, in-process |

## Stop conditions checked

None applies. The modules are not chatty (one to two methods per published interface). Every module
can be named without a technical word. No module reads another's tables. The shared kernel is owned
and tested. No microservice split is proposed.

## Round log

Five rounds were run. The checker reported `clean` in every one, at both default and `--strict`
settings. Rounds 2–5 added three production types (`RenewalInvoiceIssuer`, `CheckoutConfirmation`, and
their interfaces) and a registration line each; none added a module edge, so the declared graph is
unchanged and still acyclic. Findings M1–M5 stand as recorded; M5 was fixed in round 1.
