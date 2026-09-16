# Response to the review of `0f26dff..HEAD`

Reviewer: `cqrs-reviewer-agent`, five passes, gate scripts clean. Verdict: the shape is right, but the
feature did not work as claimed. Two HIGH findings, both reproduced here before being fixed.

| ID | Sev | Disposition |
|---|---|---|
| A1 | **HIGH** | **Fixed and guarded.** The page fetched `/swagger/v1/swagger.json` — 404 — because `UseSwaggerUI` was never given an endpoint. `SwaggerEndpoint(DocumentationPath.Document, …)` now points it at the document the application actually serves, the two addresses are one constant, and `SwaggerUiTests` loads the URL the page declares. A browser test (`DocumentationPageTests`) asserts the operation is on the page; **removing the fix fails it**, which is how the test was shown to be non-vacuous. |
| A2 | **HIGH** | **Fixed.** The documentation policy now adds the provider's origin to `connect-src`, taken from the same `IdentitySettings` the token URL comes from, so the browser-side code exchange is permitted. `SwaggerUiTests` asserts the origin is present. |
| S1 | MEDIUM | **Fixed.** The security requirement is applied to the gated operation only, by path, and **assigned** rather than appended — a second entry in that list is an alternative, not a stricter rule. A test asserts the account routes carry no requirement. |
| S2 | MEDIUM | **Fixed.** The requirement's scope is the permission the gate checks (`Authorization:CatalogRead:ClaimValue`), not the login's scope list; the login's scopes remain the flow's, which is what the Authorize dialog needs. A test asserts the requirement is exactly `read:catalog`. |
| S3 | MEDIUM | **Fixed by wording.** The transformer's remarks no longer claim the endpoints are discovered; they say plainly that these are the provider's convention and that another issuer's would be read from discovery. |
| S4 | LOW | **Fixed.** Every scope carries a description, asserted. |
| S5 | MEDIUM | **Fixed.** One `SwaggerTestSetup` now holds the identity settings and the config reader, shared by both factories and all three page tests; the reader asserts its marker was found before slicing, so a change to the page's shape fails as a statement about the page rather than an out-of-range substring. |
| A4 | MEDIUM | **Recorded, not coded.** The relaxation is bound to Development rather than to loopback. Written into the security record as residual risk, with the mitigation named. |
| A5 | MEDIUM | **Fixed.** `DocumentationPageTests` loads `/swagger/index.html` in a browser, requires the operation to be listed, and fails on any CSP refusal — the test whose absence let A1 through 104 green tests. |
| A6 | MEDIUM | **Fixed.** The documentation policy is derived from the strict one by substitution instead of being written beside it, and a test asserts the *exact* set of directives that differ: `connect-src`, `script-src`, `style-src`, and nothing else. |
| A7 | MEDIUM | **Fixed.** The third addendum is in `specs/security/REVIEW.md`: scope, why it cannot be narrower, the alternative considered and rejected, and the residual risk. |
| A8 | MEDIUM | **Fixed.** The README's Auth0 setup now names the documentation page's callback URL and `Swagger:ClientId`, where before those lived only in a verification file. |
| A9 | LOW | **Not touched.** It is in the working tree, uncommitted, and it is the user's edit. |

## What went wrong, plainly

I verified that the page and the document were *served*, and that the page's configuration contained
the right values — then reported that it worked. None of that could see the two things that mattered:
whether the page could read a document, and whether the browser could reach the provider. A browser
test asserting the *outcome* would have caught both, which is why the missing test is the finding I
should have found in my own `verify-work`.

The lesson the review recorded, and this response accepts: fetching 200s and dumping configuration is
not verification of a feature whose whole point is what a person sees. `DocumentationPageTests` is that
verification now, and removing the fix fails it.

## Baselines

| Baseline | Before | After |
|---|---|---|
| BUILD | 0 warnings 0 errors | 0 warnings 0 errors |
| NONBROWSER | 536 passed, 0 failed | **539 passed, 0 failed** (+3) |
| BROWSER | 103 passed, 1 skipped | **104 passed, 1 skipped** (+1: the page in a browser) |

## A second gate that caught me

While fixing S4 I put the configured permission into the scope descriptions as a literal. The
architecture suite failed on `AuthorizationGateTests.Nothing_but_the_policy_says_what_entitles_a_reader`:
the claim is configuration, and no claim value may be written into the application's source. The
description is now generic, and the required scope still comes from `CatalogReadClaim`. Two gates —
that one and the browser test — each caught a real defect in this change that I had not.
