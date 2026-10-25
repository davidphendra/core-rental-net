# Rephraser + Catalogue Metadata Alignment Plan

Status: proposed. No source, prompt, catalogue, or ingestion changes in this phase.

Baseline report:

```text
specs/evaluation/reports/FULL_2026-10-25_GPTOSS_3MIN.md
specs/evaluation/reports/FULL_2026-10-25_GPTOSS_3MIN.json
```

Baseline scope: all 200 cases, 136 raw passes, 136 strict passes, 33
timeouts, 61 `RETRIEVAL_MISS`, 39 `NO_CANDIDATE`, and 10 bean-bag cases with
`NO_CANDIDATE`.

## Objective

Test two coupled hypotheses:

1. Rephraser vocabulary is a material cause of retrieval misses and wrong
   product selection.
2. Catalogue metadata is not aligned with the vocabulary customers and the
   rephraser use, so even a good query cannot place the correct product close
   enough to the intended meaning.

The analysis must answer these questions for every case, passing and failing:

1. What did the rephraser emit for each category: `retrieval_query`,
   `search_terms`, `synonyms`, and `semantic_concepts`?
2. Which emitted terms occur in the catalogue `Name`, `Description`, `Tags`,
   `BestFor`, `NotFor`, or `Attributes` of the expected product?
3. Which expected-product identity details are absent from the emitted terms?
4. Which expected-product identity details are absent from the catalogue
   search text and structured metadata?
5. Did the rephraser vocabulary cause the retrieval miss, the catalogue
   metadata cause it, both, or did the expected product survive retrieval and
   fail later in reranking, composition, review, or timeout?

The output must separate rephraser, catalogue, retriever, reranker, composer,
reviewer, and timeout failures.

The result is a paired artifact set:

```text
Agent side:
  workspace-requirement-rephraser.v9.md
  optional catalogue-vocabulary guidance for the rephraser

Catalogue side:
  deterministic metadata adjustment overlay
  regenerated products.json / SeedProducts data
  rebuilt vector index
  MCP-visible product facts for composer and reviewer
```

## Data sources

### Case oracle

- `Test_Script.md`: row number, customer sentence, expected setup and SKUs.
- Evaluation report: `casePass`, failure codes, metrics, judge result.
- Per-case trace:
  `/tmp/eval-full-gptoss-20261024-231132/eval-<caseId>-r1.jsonl`.

### Rephraser evidence

From the `workspace-requirement-rephraser` stage model call:

- `retrieval_query`
- `search_terms`
- `synonyms`
- `semantic_concepts`
- category relevance
- budget and allocations
- original query
- retry feedback, when present

### Retrieval evidence

From `toolExchange` records:

- tool name;
- tool arguments, including `terms`, `category`, and `subCategory`;
- tool result text;
- which terms returned which products;
- whether the expected SKU appeared in raw search results;
- rank of the expected SKU in each result list;
- whether the expected SKU survived merge, reranking, and composition.

### Catalogue evidence

Sources:

```text
src/shared/data/products.json
src/Host/CoreRentalNet.Host/App_Data/catalog.db
src/Modules/Catalog/CoreRentalNet.Modules.Catalog.Infrastructure/Migrations/*_SeedProducts.cs
src/Tools/CoreRentalNet.CatalogPopulate
src/Tools/CoreRentalNet.CatalogIngestion
```

Table:

```text
Catalog_Product
```

Fields to index:

```text
Sku
Name
Category
SubCategory
Amount
Currency
Description
Tags
BestFor
NotFor
Attributes
```

`Attributes` is JSON. Extract at least:

```text
brand
rating
dimensions
material
color
desktype
assemblyrequired
categorypath
```

### Current prompt evidence

Current prompt:

```text
agentfoundry/src/CoreRentalNet.Agents/Features/WorkspaceSuggestion/Prompts/
  workspace-requirement-rephraser.v8.md
```

Also compare previous rephraser versions `v2` through `v8` to identify:

- rules added or removed;
- rules that contradict the observed catalogue vocabulary;
- rules that were not followed in actual outputs.

## Phase 0 — Freeze the baseline

1. Keep `FULL_2026-10-25_GPTOSS_3MIN` as the comparison baseline.
2. Do not change prompts or agent code until the analysis report is complete.
3. Record the exact baseline command, model, judge model, timeout, and trace
   directory.

## Phase 1 — Capture the rephraser input and output for all 200 cases

Current traces contain the rephraser response but not its full input. The
analysis needs the exact input to distinguish:

- a prompt-contract failure;
- a model-following failure;
- a missing input supplied by the application;
- a retry correction that was not applied.

Recommended approach:

- Add an evaluation-only trace field for the rephraser stage input, behind the
  existing `IEvaluationTraceSink` decorator.
- Do not change feature code or make production behaviour evaluation-aware.
- Do not add this data to the public API response.

Acceptance for this phase:

- All 200 case traces have either a complete rephraser input/output pair or an
  explicit reason the pair is missing.
- Timeout cases retain whatever rephraser output was produced before the
  deadline.

## Phase 2 — Build a catalogue term index

For every catalogue product, build a normalized searchable record:

```text
sku
name
description
tags
bestFor
notFor
attributes.brand
attributes.color
attributes.material
attributes.dimensions
attributes.desktype
attributes.categorypath
category
subcategory
```

Normalization:

- lowercase and punctuation-normalize;
- split dimensions into width, depth, height, and unit tokens;
- split pack and quantity expressions into numeric and noun tokens;
- preserve model numbers and brand strings;
- map known spelling variants and singular/plural forms;
- do not discard colour, material, model, or subtype tokens as stop words.

Index requirements:

- token inverted index;
- IDF/BM25 statistics over name and description;
- exact phrase matching for model numbers and multi-word varieties;
- attribute-level matching, not just full-text matching;
- embedding vectors for `retrieval_query` and `semantic_concepts` using the
  existing local embedding model at `http://localhost:8081/v1`.

Acceptance for this phase:

- A term can be traced to the exact product fields it matches.
- Attribute matches and only-textual matches are distinguishable.

### Search-channel constraint

The catalogue has two different consumers of product text:

```text
Name search:
  read by LiftiProductNameSearchService
  fields: Name + Description
  does not read Tags, BestFor, NotFor, or Attributes

Semantic search:
  reads EmbeddedText
  fields: Name + Description + Tags + BestFor + NotFor + Attributes
```

This matters for every proposed metadata adjustment:

- adding a term only to `Tags` or `Attributes` improves semantic search;
- adding a term only to `Tags` does not make name search find it;
- adding a term to `Description` affects both name search and semantic search;
- adding a term to `Name` changes the product's displayed identity and is not
  allowed merely to improve search.

The analysis must record which channel each proposed adjustment is intended
to fix.

## Phase 2b — Produce catalogue metadata adjustment candidates

This phase runs only after the term and retrieval evidence exists. It produces
candidate changes, not applied changes.

For each expected product, compare:

```text
expected identity and attributes
versus
current Name, Description, Tags, BestFor, NotFor, Attributes
```

Candidate adjustments are allowed only when the source product evidence
supports them. Do not invent a colour, material, size, pack, model, or use.

Candidate adjustment types:

| Adjustment | Search channel affected | Use when |
|---|---|---|
| `tags` alias | semantic only | the product is correctly identified but customer vocabulary is absent |
| `attributes` normalisation | semantic only | a structured attribute exists but is inconsistently expressed |
| `bestFor` / `notFor` | semantic only | use-case language is absent or misleading |
| `description` alias sentence | name and semantic | the alias must be found by the name search |
| `category` / `subCategory` | both | the existing mapping is objectively wrong |
| `name` | both | only when the published name is objectively incorrect; never for synonym padding |

Each candidate record must contain:

```text
sku
currentField
currentValue
proposedValue
evidenceCaseIds
evidenceTerm
expectedQuery
matchedCatalogueField
searchChannel
whySourceSupportsIt
risk
revisionDecision
```

The catalogue adjustment artifact is:

```text
specs/evaluation/rephraser/CATALOG_METADATA_ADJUSTMENTS.csv
```

A deterministic overlay is produced only for accepted rows:

```text
specs/evaluation/rephraser/catalog-metadata-overlay.json
```

The overlay must be source-controlled and repeatable. A hand edit to
`products.json` alone is not durable: `products.json` is generated by
`CatalogPopulate`, and the next populate run overwrites it.

### Catalogue decisions that are not rephraser fixes

- If the expected product has no term for `peony`, the catalogue may need a
  truthful alias or subtype attribute.
- If the expected product is a 3-pack but lacks a pack attribute, the
  catalogue needs a structured pack/quantity field or a truthful description
  adjustment.
- If the expected product is a bean bag but the category mapping is wrong,
  fix category/subCategory, not the rephraser.
- If the expected product cannot support the customer's claim, do not add the
  claim to metadata; mark the oracle or fixture as unsupported.

## Phase 2c — Project metadata to composer and reviewer

Catalogue metadata is not automatically visible to the workspace stages.

The current chain is:

```text
CatalogueToolProduct
  -> CatalogueSearchToolItem
  -> RetrievedWorkspaceComponentProduct
  -> WorkspaceStageInput
```

Today the chain carries `Sku`, `Name`, `Description`, and `Amount`. The
composer receives only slot, SKU, name, amount, and relevance. The reviewer
receives the bounded description.

If the catalogue is adjusted but these projections are not extended, the
composer and reviewer cannot use the improvement.

Plan a contract extension that is MCP-only and does not change the REST
`ProductDto` contract:

```text
CatalogueToolProduct.facts
  brand
  color
  material
  dimensions
  packOrQuantity
  subtype
  keyAttributes
```

Propagation:

```text
CatalogueToolProduct.facts
  -> CatalogueSearchToolItem.facts
  -> RetrievedWorkspaceComponentProduct.facts
  -> WorkspaceStageInput.Reranking / Composition / Review
  -> reranker, composer, reviewer prompts
```

Requirements:

- keep `Description` intact and bounded at the MCP boundary;
- do not remove existing properties from tool results;
- do not put raw tags or the full attribute dictionary into every prompt;
- send only compact facts needed for selection and review;
- keep REST responses unchanged;
- keep evaluation code unaware of this contract.

This projection is a prerequisite for the following claims:

```text
catalogue adjustment improves composition
catalogue adjustment improves reviewer evidence
```

## Phase 2d — Prompt impact matrix

The change is not confined to the rephraser. The following prompts and tool
contracts must be reviewed before the next full run.

| Prompt / contract | Impact | Required action |
|---|---|---|
| `workspace-request-verifier.v3.md` | None | No change. It does not use catalogue vocabulary, candidate counts, or facts. |
| `workspace-requirement-rephraser.v8.md` | Direct | Produce `v9` from the rephraser audit. |
| `catalogue-product-retriever.v6.md` | Conditional | Re-test with 12-row tool answers and updated catalogue aliases. No hardcoded 8 exists in the prompt, but the `truncated` follow-up guidance and result-count assumptions must be verified. Bump to `v7` only if the evidence requires it. |
| `catalogue-candidate-reranker.v4.md` | Direct | The pool can contain up to 15 combined candidates, and candidates gain compact facts. Produce `v5` that uses facts and does not treat a bounded description as complete. |
| `workspace-setup-composer.v3.md` | Direct | Composer currently sees only slot, SKU, name, amount, and relevance. Produce `v4` when facts are projected, so it can use colour, material, pack, quantity, and subtype. |
| `workspace-setup-reviewer.v5.md` | Direct | Reviewer currently judges from name and bounded description. Produce `v6` when facts are projected, so it can verify explicit attributes against structured facts. |
| `workspace-evaluation-judge.v2.md` | Indirect | Do not change as part of this plan unless the judge evidence contract changes. Re-check after the workspace evidence projection. |
| `SearchCatalogueTool` / `SearchSimilarityCatalogueTool` descriptions | Direct | No prompt text names 8, but the default limit and documentation must match 12. |
| `CatalogToolLimits` comments and tests | Direct | Update the default from 8 to 12 and pin it with tests. |

### Why retriever may stay at v6

The retriever prompt says "the cap" rather than 8, and it already tells the
model not to repeat or paraphrase terms. The 12-row change may therefore need
no prompt revision. It still must be tested because the follow-up rule exists
for a truncated result, and a larger cap changes when `truncated` is true.

### Why reranker, composer, and reviewer need more than a window change

They receive product evidence, not just candidate counts:

- reranker sees name, description, and amount;
- composer sees name and amount;
- reviewer sees name and bounded description.

A larger candidate pool without facts only makes those stages see more
text, not better evidence. The facts projection is what lets them use
catalogue metadata. Therefore the prompt revisions are gated on the facts
contract, not merely on the 12/15 window change.

### Prompt-change order

```text
1. retrieval window 12/15
2. catalogue metadata overlay and ingestion
3. MCP facts projection
4. reranker v5
5. composer v4
6. reviewer v6
7. rephraser v9, after its own replay gates
```

Each prompt revision must have its own before/after evidence. Do not change
all prompts at once.

## Phase 3 — Extract one analysis row per case and category

Produce a row for every case/category pair, including cases that passed and
cases that failed.

Suggested schema:

```text
caseId
rowNumber
casePass
strictPass
failureCodes
judgeEvaluated
judgePass
judgeReason
timeout
sentence
categoryKey
catalogueCategory
catalogueSubCategory
compositionSlot
retrievalQuery
searchTerms
synonyms
semanticConcepts
expectedSkus
actualSkus
expectedInRawSearch
expectedInMergedSearch
expectedInReranker
expectedInComposer
selectedSkus
missingExpectedSkus
extraSelectedSkus
lastCompletedStage
```

For each expected SKU, add:

```text
expectedSku
expectedName
expectedDescription
expectedBrand
expectedColor
expectedMaterial
expectedDimensions
expectedPackOrQuantity
expectedSubtype
expectedTags
expectedAttributes
```

The row must be generated for all 200 cases. Timeout cases must remain in the
dataset with `timeout = true`; they must not be silently dropped.

## Phase 4 — Measure term quality against catalogue evidence

For each term, calculate:

### Literal matching

- exact occurrence in `Name`;
- exact occurrence in `Description`;
- exact occurrence in `Tags`;
- exact occurrence in `BestFor` / `NotFor`;
- exact occurrence in `Attributes` values;
- match count and matched fields.

### Fuzzy and semantic matching

- token overlap and character similarity against expected product text;
- embedding cosine between the term and the expected product;
- embedding cosine between the term and the selected product;
- semantic margin:

```text
cosine(term, expected product)
-
max(cosine(term, non-expected products in the same category))
```

### Product-level coverage

- percentage of expected-product identity tokens covered by the emitted terms;
- percentage of emitted terms that match the expected product;
- attribute coverage for colour, material, brand, model, dimensions, pack or
  quantity, and subtype;
- number of invented terms not found in the catalogue;
- number of over-specific terms that return zero products;
- number of expected distinctions omitted from `search_terms` and `synonyms`;
- query-to-expected-product embedding similarity;
- query-to-selected-product embedding similarity.

### Retrieval proxy

For each term and category, record:

```text
expected SKU present in top 1 / top 12 / top 25
        (top 12 after the tool window change)
expected SKU presence after reciprocal rank fusion
expected SKU presence after the combined pool (top 15)
expected SKU presence after reranker
expected SKU presence after composer
```

The tool window and combined window are different bounds and must be
reported separately. The analysis must use the actual caps, not a new cutoff.

## Phase 4b — Raise the retrieval windows

The current tool answer is capped at eight products:

```text
src/Host/CoreRentalNet.Host/Configs/CatalogToolLimits.cs
  DefaultProductCount = 8
  MaximumProductCount = 25
```

Both catalogue tools share that default:

```text
search_catalogue
search_similarity_catalogue
```

Change the effective tool answer window from 8 to 12 for both tools:

```text
DefaultProductCount: 8 -> 12
```

Keep the explicit upper bound at 25 unless a later measurement shows a
reason to lower it. The agent calls omit `limit`, so the default is the
window it receives.

Pin and verify the combined candidate window at 15:

```text
WorkspaceComponentProductPoolPolicy
WorkspaceSuggestionWorkflowOptions
  MaximumRetrievedProductsPerComponentForReranking = 15
```

If any retriever or ledger path still applies an eight-product combined cap,
raise that cap to 15. Do not allow a 12-row tool answer to be silently reduced
back to 8 before the reranker.

Preserve these bounds:

```text
RankedTermMerger.MaximumPerTerm = 25
MaximumSelectedProductsPerComponent = 3
MaximumProductCount = 25
```

The planned effective flow is:

```text
each tool:        top 12
each component:   top 15 combined
reranker input:   top 15 combined
composer:         top 3 selected per component
```

### Why this is a separate experiment

Increasing the window changes recall and token cost at the same time. It must
be measured before it is combined with the rephraser and catalogue changes.

The retrieval-window experiment must record:

```text
expected SKU in top 8 / top 12 / top 15
raw recall by window
tool result bytes and tokens
retriever input tokens
reranker input tokens
stage latency
RETRIEVAL_MISS
NO_CANDIDATE
```

The expected improvement is that products ranked 9–12 by a tool, or 9–15
after combination, become available to the reranker instead of being discarded.
The expected cost is larger tool answers and a larger reranker context.

### Guardrails

- The tool result guard remains in place; measure whether 12-row answers stay
  below it.
- The MCP description boundary remains 150 characters.
- The final retriever call continues to receive summaries, not the full rows.
- No product property is removed from tool results.
- The reranker pool remains 15; do not increase it in the same experiment.
- The composer selection remains at most 3 per component.

## Phase 5 — Classify every failing case

Assign exactly one primary failure class and optional secondary classes:

```text
REPHRASER_OMITTED_DISTINCTION
REPHRASER_OVER_SPECIFIED
REPHRASER_INVENTED_TERM
REPHRASER_WRONG_CATEGORY
REPHRASER_WRONG_SUBCATEGORY
REPHRASER_MISSING_GENERIC_FALLBACK
RETRIEVER_RANKING
RETRIEVER_DID_NOT_SEARCH
RERANKER_SELECTION
COMPOSER_SELECTION
REVIEWER_REJECTION
TIMEOUT
UPSTREAM_ORACLE_PROBLEM
```

Rules:

- `REPHRASER_*` only when the expected product exists in the catalogue and a
  corrected term set would plausibly return it.
- `RETRIEVER_RANKING` when the expected SKU was returned by a tool but did
  not survive the cap or merge.
- `RERANKER_SELECTION` when the expected SKU survived retrieval but was not
  selected.
- `COMPOSER_SELECTION` when the expected SKU survived reranking but was not
  composed.
- `TIMEOUT` when the case exceeded 180 seconds; timeout remains a failure even
  if a late composer trace exists.
- `UPSTREAM_ORACLE_PROBLEM` when the fixture expectation is not supported by
  the catalogue or the sentence is ambiguous.

Do not classify a timeout as a rephraser failure merely because the last trace
was the rephraser.

## Phase 6 — Compare passing and failing cases

Run the same metrics on both groups.

Questions:

- Do passing cases have higher term-to-expected product coverage?
- Do passing cases rely on broad generic terms that happen to retrieve the
  expected product?
- Are there passing cases where the expected SKU was never retrieved, but the
  judge accepted a different product?
- Are there failing cases where the rephraser terms were strong and the loss
  occurred downstream?
- Are the same categories repeatedly failing:
  - plant / succulent trio;
  - Tiffany peony and banker lamps;
  - monitor packs and quantities;
  - big-and-tall chairs;
  - bean bags / RelaxZone?

This comparison is required to avoid overfitting the prompt to failed cases
only.

## Phase 7 — Audit the prompt against the evidence

For each `REPHRASER_*` class, map the observed output to a prompt clause.

Check specifically:

1. Does v8 require the exact distinguishing word from the customer sentence?
2. Does v8 permit replacing a named variety with a generic noun?
3. Does v8 require a generic fallback for every named variety?
4. Does v8 require pack, quantity, model, and subtype tokens?
5. Does v8 tell the model which catalogue fields are searched?
6. Does v8 provide enough catalogue vocabulary guidance without hardcoding
   product names?
7. Does the eight-term cap force out a necessary distinction?
8. Does the retry rule widen terms even when the correct term was present but
   ranked below the cap?
9. Does the bean-bag mapping produce a term the retriever can actually use?
10. Does the `retrieval_query` wording help the semantic search, or is it a
    generic purpose statement with no product distinction?

For each finding, record:

```text
prompt version
prompt line or clause
observed output
catalogue evidence
retrieval evidence
failure class
```

## Phase 8 — Build a rephraser-only replay harness

This is the key cost-control step. Do not run the full 200-case workflow for
every prompt experiment.

Build an offline replay that:

- takes the exact rephraser input captured in Phase 1;
- calls only the rephraser stage;
- uses the same local model and temperature;
- evaluates the emitted vocabulary against the catalogue index;
- computes the retrieval proxy metrics from Phase 4;
- runs on all 200 cases, split into train and validation by row.

Initial candidate prompt changes should be limited to evidence-backed rules:

- preserve exact brand/model/size/material/colour/variety tokens;
- require a generic category fallback;
- add catalogue-grounded synonyms for product families, not specific SKUs;
- preserve pack and quantity terms when the catalogue expresses them;
- separate `bean_bag` vocabulary from `chair`;
- prevent retry from widening a term that was correct but ranked too low;
- keep the term cap but prioritize distinguishing terms over generic ones.

Do not hardcode expected SKUs, product names, or fixture sentences into the
prompt.

## Phase 9 — Validate before the next full run

Gate the next full run on all of the following:

1. All 200 cases have analysis rows.
2. Every failing case has one primary failure class.
3. The rephraser diagnosis is supported by both:
   - term-to-catalogue evidence;
   - retrieval outcome evidence.
4. Every accepted catalogue adjustment has source evidence and a search-channel
   decision.
5. The candidate prompt improves term coverage on the training split.
6. The candidate prompt does not regress the validation split or the passing
   cases.
7. The catalogue overlay improves expected-SKU retrieval on the retrieval probe.
8. The candidate prompt does not rely on fixture-specific hardcoding.
9. The bean-bag cases produce a candidate or an explicit retrieval diagnosis.
10. The MCP facts projection is present for reranker/composer/reviewer when
    the catalog decision requires it.
11. The retrieval-window experiment has measured the effect of 12-row tool
    answers and the 15-row combined pool independently of the rephraser and
    catalog changes.
12. The next full run uses the same settings:
   ```text
   --scope all
   --concurrency 1
   --repetitions 1
   --case-timeout 180
   --judge-model gpt-oss-20b
   ```
12. Timeout cases remain dropped, not extended.

## Phase 10 — Catalogue rollover: population -> seed -> ingestion -> host

This phase is deliberately explicit because a catalogue change is not a
prompt-only change.

The current catalogue pipeline is:

```text
source data
  -> CatalogPopulate
  -> products.json / SeedProducts migration
  -> CatalogIngestion
  -> catalogue.db vector index
  -> Host
```

### 10.1 Source of truth

`src/shared/data/products.json` is generated by `CatalogPopulate`. Editing it
directly is not durable; the next populate run overwrites it.

The accepted catalogue changes must therefore be expressed as one of:

1. a deterministic metadata overlay applied after generation; or
2. a changed `CatalogPopulate` metadata prompt/source plus a pinned model and
   a reviewed diff.

The preferred option is the deterministic overlay, because it changes only
the accepted SKUs and does not depend on regenerating all 205 products with a
model.

### 10.2 Regenerate the catalogue seed

- apply the overlay to the catalogue record pipeline;
- regenerate `products.json` and the `SeedProducts` migration;
- preserve SKU identity, prices, categories, and all unchanged fields;
- verify the diff contains only accepted rows and fields;
- run catalogue loader and architecture tests.

### 10.3 Rebuild the vector index

- run `CoreRentalNet.CatalogIngestion` against the local embedding server;
- use the same embedding model and width as the host;
- if `EmbeddedText` or the vector composition changes, bump
  `ProductVectorContract.Composition` in the same change;
- verify the vector recipe, catalogue hash, vector count, and freshness check;
- restart the host so the new catalogue and index are read.

Baseline to compare against:

```text
205 products
638 vectors
```

### 10.4 Run the catalogue-only retrieval probe before the agent

For all 200 cases, run the accepted rephraser terms against the rebuilt
catalogue and measure:

```text
expected SKU in top 1 / 12 / 15 / 25
raw recall
reranker selection
composer selection
reviewer acceptance
```

Do not run the full workspace workflow until the catalogue probe shows
improvement or a clearly explained movement.

### 10.5 Re-run the rephraser replay against the rebuilt catalogue

The catalogue changes can alter term matches and semantic scores. Re-run the
rephraser replay on the new catalogue before the full agent run.

## Phase 11 — Downstream validation

If Phase 2c is activated, validate each downstream artifact independently:

### Reranker

- candidates carry compact facts;
- facts are consistent with the catalogue record;
- facts do not replace the description;
- token cost is measured.

### Composer

- the composer can see the customer's distinguishing attributes;
- the composer can distinguish a 2-pack from a single monitor;
- the composer can distinguish a trio from a single plant;
- the composer can see colour, material, subtype, and dimensions where the
  catalogue supports them.

### Reviewer

- the reviewer can verify the claim against `Name`, `Description`, and facts;
- the reviewer does not reject because a fact was omitted from a bounded
  description;
- the reviewer does not use a different product's facts.

## Phase 12 — Full evaluation

Only after Phases 0–11 pass, run the full evaluation with the same settings
and compare:

```text
raw pass
strict pass
timeouts
answer_exact
retrieval_quality
raw_retrieval_recall
RETRIEVAL_MISS
NO_CANDIDATE
JUDGE_REJECTED
bean-bag case results
```

The report must separate the effects of:

```text
rephraser prompt
catalogue metadata
vector index
MCP facts projection
12-row tool window
15-row combined pool
```

## From deliverables to rephraser artifacts

The analysis outputs are not copied into the prompt. Each output becomes a
specific decision record, and only accepted decisions become prompt or input
artifacts.

### Decision pipeline

```text
REPHRASER_CASE_MATRIX.csv
    -> failure classes and affected case IDs

REPHRASER_TERM_MATCHES.csv
    -> missing or wrong vocabulary, mapped to catalogue fields

REPHRASER_FAILURE_CLASSIFICATION.md
    -> which failures belong to the rephraser, and which do not

REPHRASER_PROMPT_AUDIT.md
    -> evidence-to-clause table with proposed rule changes

REPHRASER_V9_PROPOSAL.md
    -> exact prompt diff for rules that survived the audit

workspace-requirement-rephraser.v9.md
    -> the only prompt artifact promoted to feature code

REPHRASER_REPLAY_RESULTS.md
    -> accept or reject v9 before any full run
```

### 1. Case matrix to failure classes

`REPHRASER_CASE_MATRIX.csv` provides one row per case/category.

Each row is grouped by primary failure class. For example:

```text
REPHRASER_OMITTED_DISTINCTION
  TS-021, TS-045, TS-061, TS-081, TS-097,
  TS-133, TS-169

REPHRASER_OVER_SPECIFIED
  cases where a term returns zero products

RERANKER_SELECTION
  expected SKU retrieved but not selected

TIMEOUT
  33 cases; excluded from rephraser prompt changes
```

The grouping produces a ranked list of prompt problems. A class is eligible
for a prompt change only when it has repeated evidence across multiple cases.

### 2. Term matches to catalogue-grounded rules

`REPHRASER_TERM_MATCHES.csv` provides one row per term and catalogue field.

The analysis looks for repeated patterns:

```text
term matches Name but not Description
term matches Description but not Name
term matches Tags but not Attributes
term omits Attributes.color
term omits Attributes.material
term omits Attributes.brand
term omits Attributes.desktype
term omits pack or quantity in Name
term has zero matches in the whole catalogue
term matches too many products to distinguish
```

Each repeated pattern produces a candidate rule with evidence.

Examples of evidence-to-rule conversion:

| Evidence | Candidate rephraser rule |
|---|---|
| Named variety appears in expected `Name` but not in terms | Preserve the named variety and add the generic noun |
| `trio` appears in expected name/description but terms omit it | Preserve pack or quantity wording when the catalogue expresses it |
| `peony` or `banker` appears in expected `Name` but not in terms | Preserve the distinguishing subtype word |
| Terms use only category nouns and retrieve popular unrelated products | Prioritise distinguishing terms over generic nouns within the term cap |
| Term uses a word absent from every catalogue field | Replace with a catalogue-grounded synonym or remove it |
| Expected product is found by a term but ranked below the cap | Do not change the term; classify as retriever ranking |

### 3. Failure classification to scope control

`REPHRASER_FAILURE_CLASSIFICATION.md` prevents changes to the wrong stage.

Decision rules:

```text
REPHRASER_*        -> eligible for v9 prompt change
RETRIEVER_*        -> retriever ticket, not rephraser prompt
RERANKER_SELECTION -> reranker ticket, not rephraser prompt
COMPOSER_SELECTION -> composer ticket, not rephraser prompt
REVIEWER_REJECTION -> reviewer ticket, not rephraser prompt
TIMEOUT            -> timeout investigation, not rephraser prompt
UPSTREAM_ORACLE    -> fixture or catalogue repair, not rephraser prompt
```

This means, for example, the bean-bag cases are only assigned to the
rephraser if the trace shows that `bean_bag` terms were emitted and the
retriever still did not call the right search. If the terms were correct and
the tool was not called, the artifact is a retriever/component-mapping ticket,
not a prompt edit.

### 4. Prompt audit to exact rule changes

`REPHRASER_PROMPT_AUDIT.md` contains one row per accepted finding:

```text
findingId
failureClass
evidenceCaseIds
currentPromptVersion
currentPromptClause
observedOutput
catalogueEvidence
retrievalEvidence
proposedRule
risk
expectedMetricMovement
```

This is the bridge from evidence to prompt text. No prompt wording is changed
without a row in this table.

### 5. V9 proposal to prompt artifact

`REPHRASER_V9_PROPOSAL.md` is a diff against `v8`, not a new free-form prompt.

It contains:

- exact lines removed;
- exact lines added;
- the finding ID that justifies each change;
- examples drawn from catalogue vocabulary, never expected SKUs;
- unchanged output schema;
- unchanged category keys;
- unchanged boundary rules: no SKU, no brand invention, no price, no quantity
  invention;
- no fixture sentence copied verbatim.

The only artifact promoted to feature code is:

```text
agentfoundry/src/CoreRentalNet.Agents/Features/WorkspaceSuggestion/Prompts/
  workspace-requirement-rephraser.v9.md
```

`v8` remains available as the rollback baseline.

### 6. Conditional catalogue vocabulary artifact

If `REPHRASER_TERM_MATCHES.csv` shows repeated failures caused by missing
catalogue vocabulary, the plan adds a second artifact:

```text
WorkspaceRephraserCatalogueVocabulary
```

This is a finite, catalogue-derived vocabulary profile containing:

```text
category and subcategory names
common attribute values: colour, material, desk type
common product subtypes: executive, standing, dual motor, peony,
  banker, trio, big and tall, bean bag
```

It must not contain:

```text
expected SKUs
product names
prices
fixture sentences
```

The profile is generated by an offline script from `Catalog_Product` and
injected into the rephraser input as vocabulary guidance, not as a list of
answers.

This artifact is added only if the evidence shows the prompt rules alone
cannot supply the missing vocabulary. The decision is recorded in
`REPHRASER_PROMPT_AUDIT.md`.

### 7. Replay results to promotion decision

`REPHRASER_REPLAY_RESULTS.md` compares `v8` and `v9` on the same 200 captured
inputs.

Promotion requires:

```text
higher expected-product term coverage
higher expected-SKU raw retrieval presence
lower RETRIEVAL_MISS on the validation split
lower NO_CANDIDATE on the validation split
no regression on passing cases
no fixture-specific terms
```

If `v9` fails any gate, it is not promoted. The result goes back to
`REPHRASER_PROMPT_AUDIT.md` as a rejected finding.

## Deliverables

```text
specs/evaluation/rephraser/REPHRASER_ANALYSIS_PLAN.md
specs/evaluation/rephraser/REPHRASER_CASE_MATRIX.csv
specs/evaluation/rephraser/REPHRASER_TERM_MATCHES.csv
specs/evaluation/rephraser/REPHRASER_FAILURE_CLASSIFICATION.md
specs/evaluation/rephraser/REPHRASER_PROMPT_AUDIT.md
specs/evaluation/rephraser/REPHRASER_V9_PROPOSAL.md
specs/evaluation/rephraser/REPHRASER_REPLAY_RESULTS.md

specs/evaluation/rephraser/CATALOG_METADATA_ADJUSTMENTS.csv
specs/evaluation/rephraser/CATALOG_SEARCH_PROJECTION.md
specs/evaluation/rephraser/catalog-metadata-overlay.json
specs/evaluation/rephraser/CATALOG_ROLLOVER_REPORT.md
specs/evaluation/rephraser/CATALOG_RETRIEVAL_PROBE.md

specs/evaluation/rephraser/MCP_FACTS_PROJECTION.md
specs/evaluation/rephraser/RETRIEVAL_WINDOW_REPORT.md
specs/evaluation/rephraser/PROMPT_IMPACT_MATRIX.md
```

The case matrix and term matches must include passing and failing cases.

## Success criteria

The work is successful when it produces measurable movement in both workstreams.

### Agent side

```text
expected-product term coverage
expected-SKU raw retrieval presence
RETRIEVAL_MISS rate
NO_CANDIDATE rate
answer_exact
strict case pass
```

### Catalogue side

```text
expected-SKU presence in name search
expected-SKU presence in semantic search
expected-SKU rank in top 1 / 12 / 15 / 25
catalogue-field coverage for colour, material, model, pack, quantity, subtype
vector freshness and vector count
```

### Retrieval window side

```text
each tool returns 12 by default
combined candidate pool returns 15
no hidden 8-row cap before the reranker
expected-SKU recall at @8, @12, and @15
tool result token and latency impact
```

### Downstream side

```text
reranker receives consistent compact facts
composer can distinguish pack, quantity, colour, material, and subtype
reviewer can verify against facts instead of only a bounded description
```

and the result must not move `casePass` only through judge acceptance of
alternates.

## Out of scope for this phase

- Applying catalogue changes before the adjustment evidence and review are
  complete.
- Adding product names or SKUs to the rephraser prompt.
- Rewriting catalogue names merely to satisfy a query.
- Inventing catalogue attributes not supported by source evidence.
- Changing the judge model or judge policy.
- Changing timeout semantics.
- Running another full 200-case evaluation before the replay and catalogue
  probes pass.
- Committing any change.

## Appendix A — SeedProducts change sketch

These are illustrative shapes, not approved changes.

### A.1 `products.json` before and after

Example: a monitor that is sold as a two-pack.

Before:

```json
{
  "skuNo": "MONB086Q776F",
  "metadata": {
    "tags": ["4k", "hdmi", "led", "monitor", "monitors"],
    "attributes": {
      "brand": "Lenovo",
      "type": "monitor"
    },
    "bestFor": ["detail work", "dual-screen setups"],
    "notFor": []
  }
}
```

After:

```json
{
  "skuNo": "MONB086Q776F",
  "metadata": {
    "tags": [
      "4k",
      "hdmi",
      "led",
      "monitor",
      "monitors",
      "2-pack",
      "dual monitor",
      "two monitors"
    ],
    "attributes": {
      "brand": "Lenovo",
      "type": "monitor",
      "packQuantity": "2",
      "packType": "2-pack",
      "screenCount": "2"
    },
    "bestFor": [
      "detail work",
      "dual-screen setups",
      "multi-monitor workstations"
    ],
    "notFor": []
  }
}
```

If the name search must find `dual monitor`, the description needs a truthful sentence:

```text
Sold as a 2-pack; suitable for a dual-monitor setup.
```

Tag-only aliases improve semantic search, not the Lifti name search.

### A.2 Overlay input

```json
{
  "sku": "MONB086Q776F",
  "addTags": ["2-pack", "dual monitor", "two monitors"],
  "setAttributes": {
    "packQuantity": "2",
    "packType": "2-pack",
    "screenCount": "2"
  },
  "addBestFor": ["multi-monitor workstations"],
  "appendDescription": "Sold as a 2-pack; suitable for a dual-monitor setup."
}
```

### A.3 SeedProducts migration shape

The existing row is shaped like this; long values are shortened here:

```csharp
{
    "MONB086Q776F",
    "Lenovo D24-10 ... 2-Pack",
    3,
    4,
    572000m,
    "IDR",
    "Lenovo D24-10 ... Low Blue Light certification",
    "https://.../51ltRRiB8lL._AC_.jpg",
    false,
    58,
    "[\"4k\",\"hdmi\",\"led\",\"monitor\",\"monitors\"]",
    "[\"detail work\",\"dual-screen setups\"]",
    "[]",
    "{\"brand\":\"Lenovo\",\"rating\":\"5.0\",\"type\":\"monitor\"}"
},
```

The generated row after the overlay:

```csharp
{
    "MONB086Q776F",
    "Lenovo D24-10 ... 2-Pack",
    3,
    4,
    572000m,
    "IDR",
    "Lenovo D24-10 ... Low Blue Light certification. Sold as a 2-pack; suitable for a dual-monitor setup.",
    "https://.../51ltRRiB8lL._AC_.jpg",
    false,
    58,
    "[\"4k\",\"hdmi\",\"led\",\"monitor\",\"monitors\",\"2-pack\",\"dual monitor\",\"two monitors\"]",
    "[\"detail work\",\"dual-screen setups\",\"multi-monitor workstations\"]",
    "[]",
    "{\"brand\":\"Lenovo\",\"rating\":\"5.0\",\"type\":\"monitor\",\"packQuantity\":\"2\",\"packType\":\"2-pack\",\"screenCount\":\"2\"}"
},
```

### A.4 Other accepted shapes

Succulent trio:

```json
{
  "sku": "PLTB0BPRTB7C",
  "addTags": ["trio", "set of 3", "3-pack"],
  "setAttributes": {
    "packQuantity": "3",
    "setType": "succulent trio"
  }
}
```

Tiffany grape lamp:

```json
{
  "sku": "LMPB0BGHVFD1",
  "addTags": ["tiffany", "stained glass", "grape", "table lamp"],
  "setAttributes": {
    "style": "Tiffany",
    "pattern": "grape",
    "lampType": "table lamp"
  }
}
```

Do not add `peony` to this product.

Bean bag:

```text
No seed change.
```

`BBGB0BK4J6K8` already has `subCategory: beanbag`, beanbag tags, and
`bestFor: lounge seating`. If its search tool is not called, the failure is
a retriever/component-mapping defect, not a catalogue metadata defect.

Bow-front desk:

```json
{
  "sku": "DSKB004DOXP7",
  "addTags": ["bow front", "executive desk", "knee-hole desk"],
  "setAttributes": {
    "desktype": "bow front",
    "style": "traditional"
  }
}
```

### A.5 What the seed diff must not contain

```text
expected SKUs from Test_Script.md
expected setup sentences
one-off terms tied to one test case
invented colours, materials, pack sizes, or model numbers
changes to unchanged product fields
```

The seed change must be generated from accepted overlay rows, with the
catalogue hash and vector index rebuilt afterward.
