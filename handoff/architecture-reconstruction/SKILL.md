---
name: architecture-reconstruction
description: >
  Reconstruct the architecture behind a code change and explain it in dependency order —
  data model changes first, then the APIs built on them, then the business logic, then every
  downstream consumer, and finally the tests that prove it works. Context first, code second.
  Use when asked to "reconstruct the architecture", "explain this PR", "walk me through this
  change", "what's the architecture behind this change", or when a reviewer wants to understand
  a pull request the way an engineer thinks about systems rather than as an alphabetical file
  diff. Input is a PR number/URL (preferred), a branch/commit range, or the current working diff.
---

# Architecture Reconstruction

Given a change (usually a pull request), produce a report that explains it the way engineers
reason about systems: **from the foundation up**. A diff sorted alphabetically hides the
structure of a change; this skill recovers it.

The report always presents the change in this fixed order:

1. **Data model changes** — the new shape of the domain
2. **APIs built on top of them** — how that shape is exposed
3. **Business logic** — what the system now does with it
4. **Downstream consumers** — everyone affected, including code the diff didn't touch
5. **Tests** — the proof it works, mapped back to the layers above

Every section leads with a context paragraph (*what* changed and *why*) before showing any
code. **Context first. Code second.**

## Ecosystem repositories

Changes in this organization frequently cross service boundaries. When tracing downstream
consumers (Phase B/C), do not stop at the PR's own repository — search these sibling
repositories for consumers of any changed contract (API routes, event/message schemas, shared
DTOs, client packages):

- `UiPath/du-app`
- `UiPath/du-services`
- `UiPath/du-metering`
- `UiPath/du-agents`
- `UiPath/ai-proxy`

Use GitHub code search (`mcp__github__search_code`, scoped per repo) or locally cloned copies
when available. If a repository is inaccessible in the current session, say so explicitly in
the report's "Downstream Consumers" section rather than silently skipping it.

## Phase A — Gather the change

Collect everything needed to infer *intent*, not just content:

1. **PR metadata** — `mcp__github__pull_request_read` (method `get`): title, description,
   base/head refs, linked issues.
2. **The diff** — `pull_request_read` (method `get_files` + `get_diff`): full file list with
   patches. For very large PRs, page through all files — a partial file list invalidates the
   completeness check in Phase B.
3. **Commit history** — `mcp__github__list_commits` on the head branch: commit messages often
   reveal the build order the author actually followed.
4. **Linked issues / PR discussion** — `issue_read`, PR comments: the motivating problem in
   the author's own words.

Fallbacks when there is no PR: `git diff --stat base...head` and `git diff base...head` for a
branch or commit range; `git diff HEAD` (plus `--staged`) for the working tree.

## Phase B — Classify every changed file into exactly one layer

Bucket each file using path heuristics first, then file content (read the patch — and the full
file when the patch is ambiguous). Typical signals, by stack:

| Layer | C# / .NET | TypeScript / Node | Python |
|---|---|---|---|
| **Data model** | Entity classes, EF migrations, `DbContext` changes, DTO/record types, protobuf/OpenAPI specs, enums modeling domain state | ORM models/schemas (Prisma, TypeORM), interface/type definitions for domain objects, JSON schemas | ORM models (SQLAlchemy, Django), pydantic models, dataclasses, alembic migrations |
| **API** | Controllers, minimal-API route maps, service interfaces, gRPC service defs, SignalR hubs | Route handlers, controllers, GraphQL resolvers/typedefs, API client wrappers being *defined* | FastAPI/Flask routes, view functions, gRPC servicers |
| **Business logic** | Services, handlers (MediatR), validators, domain rules, orchestration | Services, use-case modules, state management logic | Services, tasks, pipeline steps, validators |
| **Downstream consumers** | Call sites of changed APIs, UI/view code, other services' clients, config, feature flags, DI registrations | Components consuming changed APIs/types, hooks, API-client *call sites* | Callers, notebooks, cron/job definitions, config |
| **Tests** | `*Tests.cs`, xUnit/NUnit projects | `*.test.ts`, `*.spec.ts` | `test_*.py`, `conftest.py` |

Rules:

- **Every file lands in exactly one bucket.** Files that fit none (docs, CI, build scripts,
  lockfiles) go in an explicit **Other** bucket — never silently dropped.
- A file with changes spanning layers goes in its *primary* bucket; mention the secondary role
  in prose ("this controller change also tweaks a DTO default").
- **Consumers require active search, not just diff reading.** For every changed public symbol
  (renamed API route, new required field, changed method signature), grep the PR's repo *and*
  the ecosystem repos above for call sites. Unchanged callers of changed contracts are the
  highest-risk part of any change.
- Finish with a **completeness check**: count files in the diff vs. files placed in buckets.
  The two numbers must match; the report includes the coverage table proving it.

## Phase C — Reconstruct the narrative

Fill in `assets/report-template.md`. Non-negotiables per section:

- **Overview** states the motivating problem (from the issue/PR description) in 2–4 sentences
  before anything technical, then a one-paragraph summary of the whole change.
- **Data model** explains what new shape the domain takes and *why the problem demanded it*,
  then shows the key type/schema excerpts (trimmed to the meaningful lines, not full files).
- **APIs** opens by connecting to the previous layer ("these new fields are exposed by…"),
  documents contract changes (routes, request/response shapes, versioning/back-compat), then
  excerpts.
- **Business logic** explains the behavior change in domain terms first, then the code. Call
  out invariants, edge cases handled, and error paths.
- **Downstream consumers** traces impact explicitly: who calls this, what breaks if the layer
  above is wrong, which consumers were updated in this PR vs. merely *affected* by it.
  Cross-repo findings (or access gaps) go here.
- **Tests** maps each test back to the behavior of an earlier layer ("proves the migration
  backfills…", "covers the 404 path of the new endpoint") and explicitly names behaviors from
  the layers above that have **no test** — absence of proof is a finding.
- Keep code excerpts short and cited as `path:line`; the narrative carries the report.

## Phase D — Deliver

- Write the report to a markdown file named `architecture-<repo>-pr<number>.md` (in the
  session scratchpad unless the user names a location) and present it to the user.
- Post it as a PR comment **only if explicitly asked**.
- If the user wants a deeper dive on one layer, expand that section rather than regenerating
  the whole report.
