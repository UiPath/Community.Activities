# Architecture Reconstruction: {PR title}

> **PR:** {owner}/{repo}#{number} — {url}
> **Author:** {author} · **Base:** `{base}` ← **Head:** `{head}`
> **Scope:** {N} files changed (+{additions} / −{deletions})
> **Linked issues:** {issue links, or "none"}

## Overview — the why

{2–4 sentences: the problem or need that motivated this change, in the author's terms
(from the linked issue / PR description / commit messages). No code, no file names.}

{1 paragraph: what the change does end-to-end, tracing the path the rest of this report
will follow: data model → API → logic → consumers → tests.}

---

## 1. Data model changes

{Context paragraph: what new shape the domain takes, and why the problem above demanded it.
If the PR has no data-model changes, say so in one line and explain what the change is
anchored on instead — then keep the remaining sections in order.}

{Key excerpts — trimmed to meaningful lines, cited as `path:line`.}

{Migration/back-compat notes: is existing data affected? Is the change additive or breaking?}

## 2. APIs built on them

{Context paragraph opening with the connection to layer 1: "these new fields are exposed
by…". Contract changes: new/changed routes, request/response shapes, versioning, back-compat.}

{Key excerpts.}

## 3. Business logic

{Context paragraph: the behavior change in domain terms — what the system now does that it
didn't before. Invariants, edge cases, error paths.}

{Key excerpts.}

## 4. Downstream consumers

{Context paragraph: who consumes the contracts changed above — in this repo and across the
ecosystem repos — and what breaks if the layers above are wrong.}

- **Updated in this PR:** {consumers changed by the diff}
- **Affected but unchanged:** {call sites found by searching for changed symbols — the
  highest-risk list in the report}
- **Cross-repo:** {findings per ecosystem repo; explicitly note any repo that could not be
  searched in this session}

## 5. Tests

{Context paragraph: overall testing approach of the PR.}

| Test | Proves (layer & behavior) |
|---|---|
| `{test path or name}` | {which behavior from sections 1–4 it verifies} |

**Untested behavior:** {behaviors introduced above with no covering test, or "none found".}

## 6. Other changes

{Docs, CI, build, lockfiles — one line each, or "none".}

---

## Coverage check

{N} files in diff → {N} files accounted for. ✔

| File | Layer |
|---|---|
| `{path}` | {Data model / API / Business logic / Consumer / Tests / Other} |
