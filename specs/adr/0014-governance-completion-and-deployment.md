# ADR-0014: Governance — the product owner decides done; deployment requires consent

- **Status:** Accepted (2026-09-10)
- **Decided by:** product owner

## Decision
1. **Done is declared by the product owner only.** The agent reports evidence —
   what was built, what passed, what was observed — and stops. The agent does not
   decide completion or acceptance.
2. **No deployment, publishing, pushing, or provisioning of any cloud resource
   without explicit consent.** No Azure resources. No exceptions, no inferred
   consent.
3. **Epic archiving:** an epic is moved to `specs/epics/archive/` automatically
   once it builds clean and its tests pass, with a dated entry in
   `specs/epics/archive/index.md` recording commit SHA, test counts and anything
   deliberately left out. Archiving therefore means *"mechanical criteria met"*,
   **not** *"accepted"*. If an archived epic fails review it is moved back.
4. **Deployment is deferred.** The marketing-site default is Azure App Service
   with no managed database. To keep that a configuration change rather than a
   rewrite, the database path and journal mode stay configuration-driven,
   migrations stay incremental, seeding is limited to an empty database, and the
   single-instance constraint is documented.

## Consequence recorded for later
SQLite's own documentation states **"WAL does not work over a network
filesystem"**, and App Service's only persistent writable storage is a UNC share
— so WAL is for local disk and CI only. WAL and `busy_timeout` are enabled
locally; a rollback journal (`Sqlite__JournalMode`) is required on App Service.
Because SQLite file locking over a network filesystem is hazardous, the app must
**never scale out** — one instance, always. Free and Shared tiers cannot scale
out, which makes this constraint free there.
