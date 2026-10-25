# Catalogue Search Projection

| Channel | Fields |
|---|---|
| Lifti name search | `Name` + `Description` |
| Semantic vector | `Name` + `Description` + `Tags` + `BestFor` + `NotFor` + `Attributes` |
| MCP facts | selected `Attributes` values |
| Composer / reviewer | name, bounded description, compact facts |

A tag-only alias improves semantic search only. An alias needed by name search must be present in `Description` (or, exceptionally, `Name`). Metadata changes were applied only where the product evidence supports them.
