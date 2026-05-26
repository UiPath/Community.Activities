# SonarCloud admin checklist — restore PR decoration on Community.Activities

**Audience:** SonarCloud admin for the `ui` org (whoever has `Administer` permission on the `Community.Activities` project).

**Context:** The CI pipelines have been hardened and vendored locally (PR #559). Sonar currently runs only on pushes to protected branches (`develop`, `masters/*`, `release/*`, `support/*`) — PR decoration is disabled because the SonarCloud project's main branch is still pinned to `master`, which no longer exists in the GitHub repo. This causes `Create analysis` to fail on PR builds (`Detected project binding: NOT_BOUND`).

After the steps below, Sonar PR decoration can be re-enabled with a single one-line YAML change.

---

## ☐ Step 1 — Rename the main branch from `master` to `develop`

SonarCloud → `Community.Activities` project → **Administration → Branches & Pull Requests**.

- Find the row marked as the main branch (currently `master`).
- Use the **Rename** action (preserves all history, trends, hotspot decisions, quality-gate baselines).
- New name: `develop`.

If rename isn't offered: delete `master` and let a fresh `develop` analysis become the new main branch. History is lost; do this only if rename is genuinely unavailable.

**Verify:** main branch in the Branches tab is `develop`. The project Overview still shows recent activity.

## ☐ Step 2 — Configure long-lived branch pattern

Same page (**Administration → Branches & Pull Requests**), find the **Long-lived branches pattern** setting.

- Set to: `masters/.*|release/.*|support/.*`
- Save.

**Verify:** when the next `masters/Python` or `release/*` build pushes analysis to Sonar, the branch shows up under "Long-lived branches" rather than being treated as a short-lived feature branch.

## ☐ Step 3 — Re-establish the GitHub ALM binding

SonarCloud → `Community.Activities` project → **Administration → DevOps Platform Settings**.

- Confirm the binding shows **GitHub** + **UiPath/Community.Activities**.
- If status is broken / re-auth needed, click **Reconnect** or remove the binding and re-add. May require re-installing the SonarCloud GitHub App on the UiPath org under SAML approval.

**Verify on GitHub:** https://github.com/organizations/UiPath/settings/installations → SonarCloud → "Configure" → confirms repo access to `Community.Activities` is granted and SAML-authorized.

## ☐ Step 4 — Confirm baseline analysis exists on `develop`

The first successful analysis on the new main branch creates the quality baseline that PR analyses compare against.

- Trigger any CI pipeline on `develop` (push a no-op commit, or queue a manual `develop` build of any pack).
- Watch the build — the `PublishSonar` stage should run and complete successfully.
- After it completes: SonarCloud → `Community.Activities` → Overview → confirm "Last analysis" is recent and on branch `develop`.

If this step fails with a Sonar error, **do not proceed to Step 5** — investigate first. Likely causes: the rename in Step 1 didn't take effect, or the binding in Step 3 isn't fully wired.

## ☐ Step 5 — Re-enable Sonar on PR builds

Once Steps 1–4 are green, hand back to the dev team to ship the one-line code change that brings back PR decoration:

In `Activities/.pipelines/templates/stage.start.yml` (PublishSonar stage condition) and `Activities/.pipelines/templates/stage.build.yml` (two condition parameters for `Sonar/prepare-sonar-coverage.yml` and `Sonar/upload-sonar-build-output.yml`):

Remove the trailing clause `, ne(variables['Build.Reason'], 'PullRequest')` from each `condition:` expression.

A `git grep` will find all three places quickly:

```
git grep "ne(variables\['Build.Reason'\], 'PullRequest')" Activities/
```

Commit and PR. The next PR build should complete the full Sonar flow: Build with scanner prep → Test → PublishSonar with merge-commit reconstruction + analyze + publish + PR decoration on GitHub.

---

## Quality gate sanity check (do this after Step 5)

- Open one of the next PR builds. PR comment from SonarCloud should appear with quality-gate status + coverage delta + new-code findings.
- Open SonarCloud → `Community.Activities` → check that the PR shows up under "Pull Requests" with its quality-gate status.

If decoration is missing but the analysis succeeded, check the SonarCloud project's GitHub App is still installed and the binding from Step 3 is still active.

---

## Related context

- PR that introduced the current vendored pipeline state: #559
- Commit that disabled PR-time Sonar (workaround that this checklist undoes): `1ba42f2`
- Original Sonar disable that started this saga: `2584bed` (2026-02-27)
- The Sonar tasks themselves use SonarSource's V1 (Node 10 EOL). Bumping to V2 is a separate follow-up — not blocking on these steps.
