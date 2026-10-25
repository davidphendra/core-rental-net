# MCP Facts Projection

## Implemented path

```text
CatalogueToolProduct.Facts
  -> CatalogueSearchToolItem.Facts
  -> RetrievedWorkspaceComponentProduct.Facts
  -> WorkspaceStageInput.Reranking / Composition / Review
```

## Facts

`ProductFacts` carries compact values only:

```text
brand, color, material, dimensions, type, desktype,
style, pattern, lampType, packQuantity, packType,
screenCount, setType, subtype, shape
```

Unspecified values are omitted.

## Boundaries

- `ProductDto.Facts` is `[JsonIgnore]`; the REST contract is unchanged.
- MCP tool answers include `facts` beside `description`.
- The 150-character MCP description boundary is unchanged.
- The reranker still receives the bounded description.
- The composer and reviewer receive the same compact facts.
