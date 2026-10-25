# Rephraser Failure Classification — Current Half Run

## Timeout

21 cases:

```text
TS-001 TS-005 TS-007 TS-013 TS-025 TS-031 TS-037
TS-047 TS-049 TS-067 TS-083 TS-085 TS-103 TS-119
TS-121 TS-139 TS-155 TS-157 TS-175 TS-191 TS-193
```

Most timeout cases end at `workspace-setup-reviewer` or `workspace-setup-composer`; 20 had a final `NO_CANDIDATE` workflow result.

## Bean-bag / `NO_CANDIDATE`

Five half-scope bean-bag cases remain `NO_CANDIDATE`:

```text
TS-043 TS-079 TS-115 TS-151 TS-187
```

They carry `TOOL_NOT_CALLED:RelaxZone` and `RETRIEVAL_MISS:BBGB0BK4J6K8`. This remains a retriever/component-mapping defect, not a rephraser or catalogue metadata defect.

## Wrong product

Non-timeout wrong-SKU failures include:

```text
TS-019 TS-023 TS-059 TS-095 TS-131 TS-167
```

## Judge

27 `JUDGE_REJECTED`, 0 `JUDGE_ERROR`. Judge rejection reasons are mostly empty final setups or genuine product-attribute mismatches.
