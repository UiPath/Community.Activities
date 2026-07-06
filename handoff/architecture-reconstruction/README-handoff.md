# Handoff: architecture-reconstruction skill

This directory is a **transfer vehicle only** — it does not belong in Community.Activities.
It carries a drafted Claude Code skill out of a remote session that could not reach its real
target repository, `UiPath/idp-eng-skills`. Delete this branch after pickup.

## What's in here

- `SKILL.md` — the skill: given a PR (or branch range / working diff), reconstruct the
  architecture behind the change and explain it in dependency order — data model → APIs →
  business logic → downstream consumers → tests. Context first, code second. It also searches
  the ecosystem repos (`du-app`, `du-services`, `du-metering`, `du-agents`, `ai-proxy`) for
  cross-repo consumers of changed contracts.
- `assets/report-template.md` — the report template the skill fills in, including a coverage
  table proving every file in the diff was classified.

## Pick it up locally

```bash
# in your idp-eng-skills clone
git checkout -b claude/architecture-reconstruction-tvs208

# copy this directory (minus this README) into the repo's skills location
cp -r <path-to>/handoff/architecture-reconstruction <skills-dir>/architecture-reconstruction
rm <skills-dir>/architecture-reconstruction/README-handoff.md

claude
```

Then give local Claude Code this prompt:

> Adapt the architecture-reconstruction skill I just copied into this repo to this repo's
> skill conventions — directory layout, frontmatter fields, naming; mirror the existing
> skills. Then validate it by running it against a recent merged PR in UiPath/du-agents,
> fix whatever the dry run exposes (especially the layer-classification heuristics), and
> commit and push to claude/architecture-reconstruction-tvs208.
