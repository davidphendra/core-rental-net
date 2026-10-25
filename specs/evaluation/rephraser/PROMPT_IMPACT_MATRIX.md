# Prompt Impact Matrix

| Prompt | Impact | Action taken / required |
|---|---|---|
| `workspace-request-verifier.v3.md` | None | No change. |
| `workspace-requirement-rephraser.v9.md` | Direct | v9 adds exact-phrase, pack/quantity, catalogue-field, and named-variety rules. |
| `catalogue-product-retriever.v6.md` | Conditional | Left at v6; tool window changed to 12 and must be re-evaluated. |
| `catalogue-candidate-reranker.v5.md` | Direct | v5 consumes compact facts and the wider candidate pool. |
| `workspace-setup-composer.v4.md` | Direct | v4 consumes facts and preserves pack, quantity, colour, material, subtype. |
| `workspace-setup-reviewer.v6.md` | Direct | v6 accepts facts as evidence and does not require every attribute in the bounded description. |
| `workspace-evaluation-judge.v2.md` | Indirect | Unchanged. |
| MCP tool defaults | Direct | `CatalogToolLimits.DefaultProductCount` changed from 8 to 12. |

Prompt-change order used: retrieval window, catalogue overlay, facts projection, reranker, composer, reviewer, rephraser.
