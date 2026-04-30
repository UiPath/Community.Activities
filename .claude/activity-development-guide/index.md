# UiPath Activity Development Guide

> **For**: AI coding agents, skills, and human developers.
> **Purpose**: Build correct, deployable UiPath activity packages.
> **Maintained by**: Humans and AI agents — if a task fails due to missing/incorrect guidance, update the relevant file.

---

## Architecture at a Glance

Every UiPath activity has three parts:

```
Activity (Runtime)          — what happens when the workflow runs
    ↕ property names must match
ViewModel (Design-Time)     — how the activity looks in Studio
    ↕ referenced by
Metadata (JSON)             — links activity ↔ ViewModel, defines display name/icon/category
```

**Key rule**: Property names on the ViewModel MUST exactly match the Activity's property names.

---

## Critical Pitfalls (Read First)

These are the most common sources of bugs. Every agent should know these:

1. **Context dies after `await`** — Read ALL `InArgument` values and extensions from `ActivityContext` BEFORE the first `await`. The context is disposed after that point. → [core/activity-context.md](core/activity-context.md)

2. **Property name mismatch** — ViewModel properties must match Activity properties by exact name. A mismatch silently breaks serialization. → [design/viewmodel.md](design/viewmodel.md)

3. **`InArgument<T>` defaults are NOT null** — `InArgument<int>.Get(context)` returns `0` (not `null`) when unset. Null-coalescing (`?? default`) never triggers for value types. Initialize defaults in the property declaration. → [runtime/activity-code.md](runtime/activity-code.md)

4. **`[DefaultValue]` required for new properties** — Adding a property to an existing activity without `[DefaultValue]` breaks all existing `.xaml` workflows that use it. → [core/best-practices.md](core/best-practices.md)

5. **`PrivateAssets="All"` misuse** — Use it for host-provided packages (UiPath SDK). Do NOT use it for third-party runtime dependencies, or the activity will get `FileNotFoundException` at runtime. → [core/project-structure.md](core/project-structure.md)

---

## Which File to Read

### By task

| Task | Start here | Then read |
|------|-----------|-----------|
| **Create a new activity** | [core/project-structure.md](core/project-structure.md) | [runtime/activity-code.md](runtime/activity-code.md) → [design/viewmodel.md](design/viewmodel.md) → [design/metadata.md](design/metadata.md) → [design/localization.md](design/localization.md) → [examples/complete-example.md](examples/complete-example.md) |
| **Refactor an activity** | [core/best-practices.md](core/best-practices.md) | [core/activity-context.md](core/activity-context.md) → topic file for the area being refactored |
| **Add/change a widget** | [design/widgets/index.md](design/widgets/index.md) | Individual widget file if needed |
| **Add Orchestrator integration** | [runtime/orchestrator.md](runtime/orchestrator.md) | [design/bindings.md](design/bindings.md) |
| **Write tests** | [testing/activity-testing.md](testing/activity-testing.md) | [testing/viewmodel-testing.md](testing/viewmodel-testing.md) |
| **Code review** | [core/best-practices.md](core/best-practices.md) | [core/activity-context.md](core/activity-context.md) |
| **Use Activities SDK** | [advanced/sdk-framework.md](advanced/sdk-framework.md) | Only when DI, telemetry, or bindings are needed |

### By topic

| Topic | File |
|-------|------|
| Architecture, platform context, Studio Desktop vs Web | [core/architecture.md](core/architecture.md) |
| ActivityContext, WF4, "read before await" | [core/activity-context.md](core/activity-context.md) |
| Project layout, .csproj, packaging, NuGet | [core/project-structure.md](core/project-structure.md) |
| Best practices, versioning, compatibility | [core/best-practices.md](core/best-practices.md) |
| Activity base classes, arguments, attributes | [runtime/activity-code.md](runtime/activity-code.md) |
| IExecutorRuntime, IWorkflowRuntime, feature detection | [runtime/platform-api.md](runtime/platform-api.md) |
| Orchestrator version detection, API patterns | [runtime/orchestrator.md](runtime/orchestrator.md) |
| ViewModel lifecycle, properties, partial classes | [design/viewmodel.md](design/viewmodel.md) |
| Widget types and implementation | [design/widgets/index.md](design/widgets/index.md) |
| DataSource patterns (static, dynamic, enum) | [design/datasources.md](design/datasources.md) |
| Rules and reactive dependencies | [design/rules-and-dependencies.md](design/rules-and-dependencies.md) |
| Menu actions, mode switching | [design/menu-actions.md](design/menu-actions.md) |
| Validation (property, model, preview) | [design/validation.md](design/validation.md) |
| Metadata JSON, SDK registration | [design/metadata.md](design/metadata.md) |
| Localization / Resources.resx | [design/localization.md](design/localization.md) |
| Bindings (Orchestrator, connections) | [design/bindings.md](design/bindings.md) |
| Project settings (ArgumentSetting) | [design/project-settings.md](design/project-settings.md) |
| Solutions vs project scope | [design/solutions.md](design/solutions.md) |
| Activity testing (3 levels) | [testing/activity-testing.md](testing/activity-testing.md) |
| ViewModel testing (4 approaches) | [testing/viewmodel-testing.md](testing/viewmodel-testing.md) |
| Advanced patterns (NativeActivity, bookmarks) | [advanced/patterns.md](advanced/patterns.md) |
| FilterBuilder widget | [design/widgets/filter-builder.md](design/widgets/filter-builder.md) |
| Activities SDK (DI, telemetry, bindings) | [advanced/sdk-framework.md](advanced/sdk-framework.md) |
| Service access quick reference | [reference/service-access.md](reference/service-access.md) |
| Compilation constants | [reference/compilation-constants.md](reference/compilation-constants.md) |
| Design feature flags | [reference/feature-flags.md](reference/feature-flags.md) |
| Complete worked example (all files) | [examples/complete-example.md](examples/complete-example.md) |

---

## Contributing to This Guide

When an AI agent encounters a problem not covered here, or finds incorrect guidance:

1. **Identify the right file** using the topic table above
2. **Add a "Troubleshooting" entry** at the bottom of that file with:
   - What went wrong
   - Why it went wrong
   - The correct approach
3. **Keep entries concrete** — include code snippets, not just prose
4. **Cross-reference** other files when the fix spans multiple topics
