# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Overview

UiPath Community Activities — a collection of open-source activity packages (Cryptography, Database, FTP, Java, Python, Credentials) for the UiPath automation platform. Each package follows a consistent three-layer architecture: Runtime Activity → Design-Time ViewModel → Metadata JSON.

## Build Commands

```bash
# Build the full solution
dotnet build Activities/Community.Activities.sln

# Build a specific activity pack (each has its own .sln)
dotnet build Activities/Activities.Cryptography.sln
dotnet build Activities/Activities.Database.sln
dotnet build Activities/Activities.FTP.sln
dotnet build Activities/Activities.Java.sln
dotnet build Activities/Activities.Python.sln
dotnet build Activities/Activities.Credentials.sln

# Run all tests for an activity pack
dotnet test Activities/Activities.Cryptography.sln

# Run a single test by fully qualified name
dotnet test Activities/Activities.Cryptography.sln --filter "FullyQualifiedName~EncryptTextWithAes"

# Run tests in a specific test class
dotnet test Activities/Activities.Cryptography.sln --filter "FullyQualifiedName~CryptographyTests"

# Build a NuGet package (packaging projects auto-generate on build)
dotnet build Activities/Cryptography/UiPath.Cryptography.Activities.Packaging/UiPath.Cryptography.Activities.Packaging.csproj
```

## Test Framework

- **xUnit** with `Moq` for mocking and `Shouldly` for assertions
- Test parallelization is **disabled** (see `Activities/xunit.runner.json`)
- Activities are tested via `WorkflowInvoker`: create the activity, set arguments, call `invoker.Invoke()`, assert on output dictionary
- Test project naming convention: `UiPath.{Category}.Activities.Tests`

## Architecture

### Activity Pack Structure

Each activity category follows this layout:
```
{Category}/
├── {Category}.build.props                    # Version and metadata for this pack
├── UiPath.{Category}/                        # Core library (helpers, enums)
├── UiPath.{Category}.Activities/             # Activity classes (runtime logic)
│   ├── NetCore/ViewModels/                   # ViewModel classes (design-time UI)
│   ├── Properties/                           # .resx localization files
│   └── Resources/
│       ├── Icons/                            # SVG icons
│       └── ActivitiesMetadata.json           # Links activities ↔ ViewModels
├── UiPath.{Category}.Activities.Tests/       # xUnit tests
└── UiPath.{Category}.Activities.Packaging/   # NuGet package definition
```

### Three-Layer Pattern

1. **Activity** (`CodeActivity<T>`): Defines `InArgument`/`OutArgument` properties, implements `Execute()`, validates in `CacheMetadata()`. Runtime telemetry via `#if ENABLE_DEFAULT_TELEMETRY`.

2. **ViewModel** (`DesignPropertiesViewModel`): Linked to activity via `[ViewModelClass]` attribute on a partial class. Property names **must exactly match** the activity's argument names. Configures widgets, visibility rules, and menu actions in `InitializeModel()` / `InitializeRules()`.

3. **Metadata** (`ActivitiesMetadata.json`): Registers activity → ViewModel mapping, display names (resource keys), icons, and property metadata.

### Shared Projects

Code reuse via C# Shared Projects (`.shproj`/`.projitems`) in `Activities/Shared/`:
- `UiPath.Shared` — core utilities
- `UiPath.Shared.Activities` — base activity classes and attributes
- `UiPath.Shared.Telemetry` — telemetry integration

### Build Infrastructure

- **Target frameworks**: `net6.0` (portable/Studio Web) and `net6.0-windows` (Studio Desktop), defined in `Activities/Directory.build.props` as `PortableFramework` and `WindowsFramework`
- **Central dependency versions**: `Activities/Directory.build.targets` — update versions there, plus Examples and Templates
- **Per-pack versioning**: `{Category}.build.props` — `{Major}.{Minor}.{Build}-dev.{Minutes}` in Debug, `{Major}.{Minor}.0` in Release
- **CI/CD**: Azure Pipelines configs in `Activities/.pipelines/`
- **Code analysis**: `Activities/UiPath.Activities.ruleset`

## Key Conventions

- All user-facing strings go in `.resx` files; activities use `[LocalizedDisplayName]`, `[LocalizedDescription]`, `[LocalizedCategory]` attributes
- New properties must specify `[DefaultValue]` for forward compatibility
- Obsolete properties get `[Obsolete]` + `[Browsable(false)]` — do not remove them or change behavior
- Breaking changes (public contract, behavior, exceptions) require discussion with repo owners
- Assembly version conflicts are treated as errors (`MSBuildWarningsAsErrors: MSB3277`)

## Development Guide

The comprehensive activity development guide lives in `.claude/activity-development-guide/` (start with `.claude/activity-development-guide/index.md`). It covers all patterns in depth: activity code, ViewModel code, widgets, rules, menu actions, validation, metadata, localization, bindings, testing, and complete examples. **Reference it when creating or modifying activities.**
