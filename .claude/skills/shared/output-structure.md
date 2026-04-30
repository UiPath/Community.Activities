# Output Structure: Package Docs in NuGet

This reference describes how activity documentation files are structured inside NuGet packages and how UiPath Studio extracts them for AI coding assistants.

## How It Works (Studio Side)

UiPath Studio PR [#26694](https://github.com/UiPath/Studio/pull/26694) introduced `PackageDocsSync` — a service that automatically extracts documentation from installed NuGet packages into the project's `.local/docs/` directory.

### Extraction Flow

1. **Trigger**: When a project is opened or dependencies change, `PackageDocsObserver` fires
2. **Scan**: `PackageDocsSync` iterates all project packages looking for a `content/docs/` folder inside each `.nupkg`
3. **Mirror**: For packages that have docs, it copies the entire `content/docs/` tree into `.local/docs/packages/<PackageId>/`
4. **Manifest**: A `manifest.json` file tracks which package versions have been extracted, enabling incremental sync (only re-extract when versions change)
5. **Cleanup**: When a package is removed from the project, its docs folder is deleted

### Project-Level Structure (after extraction)

```
MyProject/
├── .local/
│   └── docs/
│       ├── manifest.json                          # {"UiPath.Mail.Activities": "2.5.10", ...}
│       └── packages/
│           ├── UiPath.Mail.Activities/
│           │   ├── overview.md
│           │   ├── coded/                        # Coded workflow docs (per package)
│           │   │   └── coded-api.md                  # Service API reference
│           │   └── activities/
│           │       ├── SendMailX.md               # XAML activity docs (per activity)
│           │       ├── GetOutlookMailMessages.md
│           │       └── ...
│           ├── UiPath.UIAutomation.Activities/
│           │   ├── overview.md
│           │   ├── coded/
│           │   │   └── coded-api.md
│           │   └── activities/
│           │       ├── Click.md
│           │       ├── TypeInto.md
│           │       └── ...
│           └── ...
├── project.json
├── Main.xaml
└── ...
```

## How to Include Docs in a NuGet Package

### Source Location (in this repository)

Place documentation files inside the project that **produces the published `.nupkg`** — i.e., the project whose `PackageId` matches the package users install.

> **How to identify the correct project:** Look for the `.csproj` or `.nuspec` that defines a `<PackageId>` (e.g., `UiPath.Excel.Activities`, `UiPath.WebAPI.Activities`). This is NOT always the `*.Package.csproj` — some domains split packaging into design-time and runtime projects. For example:
> - **Excel**: `UiPath.Excel.Activities.Package/` has `PackageId=UiPath.Excel.Activities` — docs go here
> - **Web**: `UiPath.Web.Activities.Package.Design/` has `PackageId=UiPath.WebAPI.Activities` — docs go here (NOT the `UiPath.Web.Activities.Package/` wrapper which has `IsPackable=false`)
>
> When in doubt, check which `.csproj` has `<PackageId>` and is packable.

```
{DomainRoot}/
├── {PublishedPackageProject}/          # The project with <PackageId> that produces the .nupkg
│   ├── *.nuspec  OR  *.csproj
│   └── docs/                          # <-- documentation source
│       ├── overview.md
│       ├── coded/                 # Coded workflow docs (if domain has coded API)
│       │   └── coded-api.md
│       └── activities/
│           ├── ActivityOne.md
│           └── ActivityTwo.md
├── {ActivityProject}/
│   └── ... (source code)
└── ...
```

### For `.nuspec`-Based Packages

Add a `<file>` entry to the nuspec:

```xml
<package>
  <metadata>
    <!-- ... -->
  </metadata>
  <files>
    <!-- existing files ... -->

    <!-- Activity documentation -->
    <file src="docs\**\*.md" target="content\docs" />
  </files>
</package>
```

### For SDK-Style `.csproj` Packages

Add an `<ItemGroup>` to the `.csproj` that produces the published package:

```xml
<!-- Activity documentation for AI coding assistants -->
<ItemGroup>
  <None Include="docs\**\*.md" Pack="true" PackagePath="content\docs\" />
</ItemGroup>
```

### Result Inside the .nupkg

After packing, the `.nupkg` ZIP will contain:

```
MyPackage.1.0.0.nupkg
├── lib/
│   └── ... (DLLs)
├── content/
│   └── docs/
│       ├── overview.md
│       ├── coded/                         # Coded workflow docs
│       │   └── coded-api.md                   # Service API reference
│       └── activities/
│           ├── ActivityOne.md             # XAML activity docs (per activity)
│           └── ActivityTwo.md
└── ...
```

## Manifest Format

The `manifest.json` is a simple JSON object mapping package IDs to versions:

```json
{
  "UiPath.Mail.Activities": "2.5.10",
  "UiPath.UIAutomation.Activities": "25.10.21",
  "UiPath.Excel.Activities": "3.3.1"
}
```

This enables Studio to:
- Skip re-extraction when versions haven't changed (fast path)
- Detect removed packages and clean up stale docs
- Handle corrupt manifests gracefully (re-extracts everything)

## Constants

From the Studio codebase:
- `WorkflowProjectConstants.LocalDataFolderName` = `".local"`
- `WorkflowProjectConstants.PackageDocsFolderName` = `"docs"`
- `WorkflowProjectConstants.PackageContentFolder` = `"content"` (the standard NuGet content folder)

The full extraction path is: `{nupkg}/content/docs/` → `{project}/.local/docs/packages/{PackageId}/`
