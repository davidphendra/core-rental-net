# 0003 — The agent repository is organised by extension mechanism, not by feature slice

**Status:** accepted
**Date:** 2026-09-18
**Deciders:** product owner, engineer

## Context

The agent repository (`agentfoundry/`) exists to serve **one capability**: turning a sentence into
workspace candidates. It is a deployable unit beside the application, with its own solution and exactly
one shared artefact — the contract schemas, per ADR 0002.

Its content varies along these axes, each extended a different way:

| Axis | Extended by |
|---|---|
| Agent | building it (options + factory) |
| Agent profile | adding data (one record per role) |
| Prompt | editing content (a versioned file, embedded at build) |
| Guardrail | platform filters in `infra/`; customer-facing output hygiene in the **application**, not here |
| Workflow | changing the graph (edges, executors, state) |
| Contract | editing a schema shared with the application |

Two axes are deliberately **absent**: there are no **tools** (the agents are tool-less, which is why
the safety posture can be structural) and there are no **skills** unless an evaluation demands one. A
third absence follows from ADR 0002: **customer-facing text handling** — the incremental reading of the
model's stream and the hygiene applied before display — belongs to the application, where the display
is, and must not appear here.

The question this record settles: should the repository instead be organised **by feature (vertical
slices)**?

## Decision

**Organise by extension mechanism. Do not adopt in-repo vertical slices.** The project and deployable
boundary **is** the feature boundary; if a second capability arrives, the default answer is a **second
hosted agent**, not a second slice inside this one.

```
agentfoundry/
├── shared/contracts/     the one artefact shared with the application
├── prompts/              prompt content, version in the filename, embedded into the assembly
└── src/…/
    ├── Composition/      the only Foundry-aware code (DI, hosting)
    ├── Agents/           profile, roster, factory — participants, not roles-as-classes
    ├── Workflows/        graph, executors, state, events, checkpointing
    ├── Prompts/          the loader that gives a prompt its version
    ├── Contracts/        mirrors of shared/contracts
    └── Observability/    the run's usage figures
```

**Prefer composition over inheritance for roles.** `AgentProfile` is data; a factory builds; the
framework's `AIAgent` is the abstraction every caller depends on. There is no
`RephraserAgent : SuggestorAgent`.

**Adopt slices the moment any of these becomes true:** one agent project hosts two or more capabilities
sharing hosting, workflow and DI but differing in prompts or guardrails; two capabilities must ship and
deploy together; or a capability's prompt set outgrows a handful of files. When adopted, the shape is
two levels — capability slice outside, mechanisms inside — with only genuinely cross-slice concerns
(guardrails, telemetry, the prompt loader, the contracts) in a shared folder.

## Consequences

**Good**

- One capability, one place to look; no slice ceremony for a boundary that does not exist yet.
- Adding a role is a roster entry, not a class hierarchy.
- Adding a concern touches one folder and no existing profile.
- `AIProjectClient` appears exactly once, in the composition root.

**Bad**

- The project is **self-contained for code deploy**: it declares exact package versions and does not
  inherit the root's central pinning, so a repository-wide version policy does not reach the agent.
- If the repository later hosts several capabilities, the slice boundary must be introduced
  deliberately; that is the one change this layout does not make additive.
- The absence of a `Tools/` folder is a decision, not an oversight: adding a tool reverses the
  structural safety posture of ADR 0002 and must be argued for on its own.

## Alternatives rejected

| Alternative | Why rejected |
|---|---|
| Vertical slices inside this repository, now | One capability makes one slice; the benefit tracks feature count and there is no feature count |
| Slicing the shared kernel per capability | Duplicates guardrails, telemetry and contracts, or recreates a shared folder anyway |
| A base class per role | Couples two roles for no reuse; MAF uses inheritance for new *kinds* of agent, not domain roles |
| A hand-built Bridge for provider independence | `AIAgent` ⟷ `IChatClient` already is that bridge |
| Reorganising the application `CoreRentalNet` the same way | Its modules already are its slices, and its style is fixed by its architecture tests |

## Notes

Evidence considered: Microsoft's own guidance documents layered, Clean and microservices as styles and
carries this idea only as **feature slices** (MSDN Magazine, September 2016) and in a .NET show; the
canonical vertical-slice samples carry few contributors while the layered reference templates carry
many. The pattern's own definition — *minimise coupling between slices, maximise coupling in a slice*,
coupling along the axis of change — is what this record applies, and the axis of change here is the
mechanism.
