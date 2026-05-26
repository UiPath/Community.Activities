# SonarCloud admin checklist — restore PR decoration on Community.Activities

**Audience:** SonarCloud admin for the `ui` org (whoever has `Administer` permission on the `UiPath_Community.Activities` project).

**Context:** The CI pipelines have been hardened and vendored locally (PR #559). Sonar currently runs only on pushes to protected branches (`develop`, `masters/*`, `release/*`, `support/*`) — PR decoration is disabled because of a stale Sonar branch model, not a missing binding.

## What we already verified (no admin action needed for these)

- ✅ **Project key is `UiPath_Community.Activities`** (in org `ui`). Confirmed by URL: `https://sonarcloud.io/summary/new_code?id=UiPath_Community.Activities&pullRequest=559`. Pipelines now point at this key.
- ✅ **Project binding is healthy.** Last Sonar scan log shows `Detected project binding: BOUND` against `UiPath_Community.Activities`. The GitHub ALM link is intact — no need to reconnect the SonarCloud GitHub App or re-grant SAML access.
- ✅ **Token and org slug are correct.** `prepare-sonar-coverage.yml` uses `organization: ui` and the token authenticates. Project lookup, settings, and branch config all load successfully.

## What's actually blocking PR decoration

PR builds fail at `INFO: Create analysis` even though everything before it succeeds. The narrow cause is the **stale main branch**: SonarCloud's main branch is still `master` (last analyzed ~6 years ago), and the project's loaded extensions (`architecture`, `a3s`, `sca`) cannot construct a coherent analysis for a PR targeting `develop` against that stale baseline.

The optimistic test (commit `2be7a4d`, reverted in `d4e3bd3`) confirmed this empirically — once the project key was right and the binding was BOUND, the `Create analysis` step still failed in the same place.

## What you need to do

The critical step is **Step 1** (main branch rename). Steps 2 and 3 are nice-to-haves.

### ☐ Step 1 — Rename the main branch from `master` to `develop` (CRITICAL)

SonarCloud → `UiPath_Community.Activities` project → **Administration → Branches & Pull Requests**.

- Find the row marked as the main branch (currently `master`).
- Use the **Rename** action — preserves all history, trends, hotspot decisions, quality-gate baselines.
- New name: `develop`.

If rename isn't offered: delete `master` and let a fresh `develop` analysis become the new main branch. History is lost — only do this if rename is genuinely unavailable.

**Verify:** main branch in the Branches tab is `develop`.

### ☐ Step 2 — Configure long-lived branch pattern (nice-to-have)

Same page, find **Long-lived branches pattern** (or "Reference branches" in newer UI).

- Set to: `masters/.*|release/.*|support/.*`
- Save.

This makes `masters/Python`, `release/Cryptography/*`, etc. show up as long-lived branches with their own quality-gate state, rather than being treated as throwaway feature branches.

### ☐ Step 3 — Trigger a baseline analysis on `develop`

The first analysis on the renamed main branch creates the baseline that PR analyses compare against.

- Push any commit to `develop` (or queue a manual `develop` build of any pack — e.g., Credentials is the simplest with no runtime tests).
- Watch the build — `PublishSonar` stage should complete cleanly.
- Verify on SonarCloud: `UiPath_Community.Activities` → Overview → "Last analysis" timestamp is recent and the branch dropdown shows `develop`.

If this fails with a Sonar error, **do not proceed to Step 4** — investigate first. Most likely: Step 1's rename didn't take effect, or there's a long-lived-branch pattern conflict.

### ☐ Step 4 — Hand back to dev team for the one-line revert

Once Steps 1–3 are verified, dev team runs:

```
git grep "ne(variables\['Build.Reason'\], 'PullRequest')" Activities/
```

Finds 3 hits. Remove `, ne(variables['Build.Reason'], 'PullRequest')` from each. Commit, push, open PR (or push to existing feature branch).

The next PR build should complete the full Sonar flow: Build with scanner prep → Test → PublishSonar with merge-commit reconstruction + analyze + publish + PR decoration on GitHub.

## Quality-gate sanity check (after Step 4)

- Open one of the next PR builds. PR comment from SonarCloud should appear with quality-gate status + coverage delta + new-code findings.
- SonarCloud → `UiPath_Community.Activities` → **Pull Requests** tab → PR shows up with its quality-gate status.

## Important caveat — first PR analyses may show a backlog

Once Sonar resumes, the first develop analysis after 3 months of inactivity (Sonar was disabled on 2026-02-27 by commit `2584bed`) will create the new baseline. Subsequent PRs analyze "new code since baseline."

You may want to set the **"New Code" definition** before merging the first PR after restoration, to avoid surfacing ~3 months of accumulated drift as "new code":

- Project → **Administration → New Code** → set to **"Specific date"** with a date just before the first restored analysis.

Without this, the first few PRs may fail their quality gate purely because the new-code definition catches too much.

## Related context

- PR with the vendored pipelines + this checklist: #559
- Commit that disabled PR-time Sonar (the workaround Step 4 undoes): `d4e3bd3` (re-applied after `1ba42f2` was reverted by `2be7a4d` and re-reverted by `d4e3bd3`)
- Commit that fixed the project key from `Community.Activities` to `UiPath_Community.Activities`: `baf6a73`
- Original Sonar disable that started this saga: `2584bed` (2026-02-27)
- The SonarCloud tasks use SonarSource's V1 (Node 10 EOL warning). Bumping to V2 is a separate follow-up — not blocking these steps.
