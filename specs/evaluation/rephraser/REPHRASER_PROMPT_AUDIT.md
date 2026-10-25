# Rephraser Prompt Audit

## Accepted v9 changes

| Finding | Evidence | v9 rule |
|---|---|---|
| Compound requirements were split into weaker terms | TS-002/016 `big chair` + `tall chair`; TS-036/131 `dual monitor` | Keep `big and tall`, `dual motor`, `2-tier`, `bow front`, `L-desk` together. |
| Pack/set wording was not consistently preserved | TS-021/045/117/189 succulent trio; monitor 2-pack cases | Keep `trio`, `set of 3`, `2-pack`, `dual monitor`, and the generic noun. |
| Named varieties needed the catalogue's words | TS-034/070/106/142/178 peony; TS-046/082 banker | Keep exact variety words plus the generic item noun. |
| Catalogue field boundary was not explicit | Actual outputs used terms absent from names/descriptions | State that name search reads name and description; tags/attributes feed semantic search. |
| Retry could widen a correct term | Existing retry rule | Do not widen when the term was correct but ranked below the cap. |

## Not changed

- Category keys.
- Output schema.
- No-SKU/no-price/no-invention boundaries.
- No test changes.

See `workspace-requirement-rephraser.v9.md`.
