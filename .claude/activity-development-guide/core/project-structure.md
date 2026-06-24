# Project Structure

> **When to read this**: When setting up a new activity package from scratch, or when troubleshooting build/packaging issues. This file covers the solution layout, NuGet configuration, .csproj templates, and the packaging pipeline.

---

## Recommended Solution Layout

```
MyActivities/
+-- MyActivities.sln
+-- nuget.config                             # NuGet feed configuration (UiPath Official feed)
+-- MyActivities/                            # Main activity + ViewModel project
|   +-- MyActivities.csproj
|   +-- Activities/
|   |   +-- MyActivity.cs                    # Runtime activity class
|   +-- ViewModels/
|   |   +-- MyActivityViewModel.cs           # Design-time ViewModel
|   +-- Resources/
|   |   +-- ActivitiesMetadata.json          # Activity metadata (single file for cross-platform)
|   |   +-- ActivitiesBindings.json          # Orchestrator bindings (if needed)
|   |   +-- Resources.resx                   # Localized strings
|   |   +-- Resources.Designer.cs            # Generated — commit for CLI builds (see note below)
|   |   +-- Icons/
|   |       +-- my-activity.svg              # Activity icon (SVG)
|   +-- Helpers/
|       +-- ActivityContextExtensions.cs     # Runtime logging helper
+-- MyActivities.Packaging/                  # NuGet packaging project
|   +-- MyActivities.Packaging.csproj
+-- MyActivities.Tests/                      # Test project
|   +-- MyActivities.Tests.csproj
|   +-- Unit/
|   |   +-- MyActivityUnitTests.cs
|   +-- Workflow/
|       +-- MyActivityWorkflowTests.cs
+-- Output/
    +-- Packages/                            # Generated .nupkg files (git-ignored)
```

For packages with platform-split activities, replace `ActivitiesMetadata.json` with `ActivitiesMetadataPortable.json` and `ActivitiesMetadataWindows.json`.

---

## NuGet Feed Configuration

UiPath SDK packages (`System.Activities.ViewModels`, `UiPath.Workflow`) are published on the UiPath official NuGet feed. Create a `nuget.config` at the solution root:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="UiPath Official"
         value="https://uipath.pkgs.visualstudio.com/Public.Feeds/_packaging/UiPath-Official/nuget/v3/index.json" />
  </packageSources>
</configuration>
```

Without this file, `dotnet restore` will not find the UiPath SDK packages.

---

## Main Project .csproj

This is the project that contains your activity classes, ViewModels, resources, and metadata.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <!-- Distinct PackageId avoids "Ambiguous project name" error with packaging project -->
    <PackageId>MyCompany.MyActivities.Library</PackageId>
    <IsPackable>false</IsPackable>
    <RootNamespace>MyCompany.MyActivities</RootNamespace>
  </PropertyGroup>

  <!-- Embed metadata JSON(s) and icons as resources in the DLL.
       Studio reads these at design time to discover activities. -->
  <ItemGroup>
    <None Remove="Resources\ActivitiesMetadata.json" />
    <EmbeddedResource Include="Resources\ActivitiesMetadata.json" />
    <EmbeddedResource Include="Resources\Icons\*.svg" />
  </ItemGroup>

  <!-- SDK and framework packages.
       PrivateAssets=All: needed at compile time, but NOT bundled in nupkg
       because the UiPath Robot/Studio host already provides these assemblies. -->
  <ItemGroup>
    <PackageReference Include="System.Activities.ViewModels" Version="1.20251103.2" PrivateAssets="All" />
    <PackageReference Include="UiPath.Activities.Api" Version="24.10.1" PrivateAssets="All" />
    <PackageReference Include="UiPath.Workflow" Version="6.0.0-*" PrivateAssets="All" />
  </ItemGroup>

  <!-- Resources.Designer.cs: PublicResXFileCodeGenerator only runs inside Visual Studio.
       For CLI builds (dotnet build, CI), generate this file once and commit it to source.
       Visual Studio will regenerate it automatically when the project is opened. -->
  <ItemGroup>
    <Compile Update="Resources\Resources.Designer.cs">
      <DependentUpon>Resources.resx</DependentUpon>
      <DesignTime>True</DesignTime>
      <AutoGen>True</AutoGen>
      <CustomToolNamespace>MyCompany.MyActivities</CustomToolNamespace>
    </Compile>
  </ItemGroup>
  <ItemGroup>
    <EmbeddedResource Update="Resources\Resources.resx">
      <LastGenOutput>Resources.Designer.cs</LastGenOutput>
      <Generator>PublicResXFileCodeGenerator</Generator>
      <CustomToolNamespace>MyCompany.MyActivities</CustomToolNamespace>
    </EmbeddedResource>
  </ItemGroup>
</Project>
```

**`System.Activities.ViewModels` version**: Every version matching `1.0.0-*` is a prerelease build. The stable release line starts at `1.20251103.2` (scheme: `1.YYYYMMDD.patch`). The `1.0.0-alpha` builds are missing `PersistValuesChangedDuringInit`, `DefaultWidget`, and `ViewModelWidgetType` — activities using these APIs won't compile.

**Important**: The `CustomToolNamespace` must match the project's root namespace so the generated `Resources` class is accessible from your code.

### Platform-Split Metadata

For packages that need separate Windows and Portable metadata, embed both files instead:

```xml
<None Remove="Resources\ActivitiesMetadataPortable.json" />
<None Remove="Resources\ActivitiesMetadataWindows.json" />
<EmbeddedResource Include="Resources\ActivitiesMetadataPortable.json" />
<EmbeddedResource Include="Resources\ActivitiesMetadataWindows.json" />
```

---

## Packaging Project .csproj

The packaging project is a thin project that references the main library and produces the `.nupkg`. It does not contain any code. The `PackageTags` value `UiPathActivities` is **required** for UiPath Studio to recognize the package as an activity package.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <PropertyGroup>
    <GeneratePackageOnBuild>True</GeneratePackageOnBuild>
    <!-- Dynamic versioning: Release gets fixed version, Debug gets unique timestamp -->
    <VersionBuild>$([System.DateTime]::UtcNow.DayOfYear.ToString("F0"))</VersionBuild>
    <VersionRevision>$([System.DateTime]::UtcNow.TimeOfDay.TotalMinutes.ToString("F0"))</VersionRevision>
    <VersionPrefix Condition="'$(Configuration)' == 'Release'">1.0.0</VersionPrefix>
    <VersionPrefix Condition="'$(Configuration)' == 'Debug'">1.0.$(VersionBuild)-dev.$(VersionRevision)</VersionPrefix>
    <PackageId>MyCompany.MyActivities</PackageId>
    <Authors>MyCompany</Authors>
    <Description>My custom UiPath activities package</Description>
    <PackageTags>UiPathActivities</PackageTags>
    <PackageOutputPath>..\Output\Packages\</PackageOutputPath>
    <TargetsForTfmSpecificBuildOutput>AddDlls</TargetsForTfmSpecificBuildOutput>
    <ProduceReferenceAssembly>False</ProduceReferenceAssembly>
  </PropertyGroup>

  <!-- Include the main library DLL (and PDB in Debug) in the package -->
  <Target Name="AddDlls">
    <ItemGroup Condition="'$(Configuration)' == 'Debug'">
      <BuildOutputInPackage Include="$(OutputPath)MyCompany.MyActivities.pdb" />
    </ItemGroup>
    <ItemGroup>
      <BuildOutputInPackage Include="$(OutputPath)MyCompany.MyActivities.dll" />
    </ItemGroup>
  </Target>

  <!-- Remove the packaging project's own (empty) DLL from the package -->
  <Target Name="RemoveMetaDll" AfterTargets="BuiltProjectOutputGroup">
    <ItemGroup>
      <BuiltProjectOutputGroupOutput Remove="@(BuiltProjectOutputGroupOutput)" />
    </ItemGroup>
  </Target>

  <!-- Clean old packages before building new ones -->
  <Target Name="CleanPackageFiles" BeforeTargets="Build">
    <ItemGroup>
      <PackageFilesToDelete Include="$(PackageOutputPath)\$(PackageId)*.nupkg" />
    </ItemGroup>
    <Delete Files="@(PackageFilesToDelete)" ContinueOnError="WarnAndContinue" />
  </Target>

  <ItemGroup>
    <ProjectReference Include="..\MyActivities\MyActivities.csproj">
      <PrivateAssets>All</PrivateAssets>
    </ProjectReference>
  </ItemGroup>

  <!-- Packages the activity needs at runtime that are NOT already provided
       by the UiPath executor/Studio host. Only list packages here that the
       consumer must restore themselves. -->
  <ItemGroup>
    <!-- Example: if your activity depends on Newtonsoft.Json at runtime -->
    <!-- <PackageReference Include="Newtonsoft.Json" Version="13.0.3" /> -->
  </ItemGroup>
</Project>
```

---

## Understanding PrivateAssets="All"

`PrivateAssets="All"` on a `PackageReference` means: "I need this package at compile time, but do **not** include it as a dependency in the nupkg -- I expect the host process to already have it loaded."

### When to USE PrivateAssets="All"

UiPath platform packages -- always. These are provided by the Robot/Studio host at runtime:

- `UiPath.Activities.Api`
- `UiPath.Workflow`
- `UiPath.Activities.Contracts`
- `System.Activities.*`

```xml
<!-- Correct: host-provided packages -->
<PackageReference Include="UiPath.Activities.Api" Version="24.10.1" PrivateAssets="All" />
<PackageReference Include="UiPath.Workflow" Version="6.0.0-*" PrivateAssets="All" />
```

### When to OMIT PrivateAssets (let dependency flow into nupkg)

Third-party packages your activity needs at runtime that are **not** provided by the executor/Studio:

```xml
<!-- Correct: runtime dependency flows into nupkg -->
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
<PackageReference Include="RestSharp" Version="110.2.0" />
<PackageReference Include="MailKit" Version="4.3.0" />
```

If a package is not provided by the host and you mark it `PrivateAssets="All"`, the activity will fail at runtime with `FileNotFoundException`.

### How it flows through the packaging project

The packaging project's `ProjectReference` to the main library uses `PrivateAssets="All"` to prevent the packaging project's own (empty) assembly from becoming a dependency. The `AddDlls` target includes the real library DLL instead. Third-party runtime dependencies from the main library flow through to the nupkg automatically as long as they are **not** marked `PrivateAssets="All"` in the main library.

---

## Building the Package

```bash
# Build and produce .nupkg (output in Output/Packages/)
dotnet build MyActivities.Packaging/MyActivities.Packaging.csproj -c Release

# Or build the entire solution
dotnet build MyActivities.sln -c Release
```

The `.nupkg` file will be in `Output/Packages/`. Install it in UiPath Studio via the Package Manager by adding a local feed pointing to that directory.

---

## Creating the Solution File

```bash
dotnet new sln -n MyActivities
dotnet sln add MyActivities/MyActivities.csproj
dotnet sln add MyActivities.Packaging/MyActivities.Packaging.csproj
dotnet sln add MyActivities.Tests/MyActivities.Tests.csproj
```

---

## Cross-References

- For metadata file format, see `../design/metadata.md`
- For localization setup with Resources.resx, see `../design/localization.md`
- For test project setup, see `../testing/testing.md`

---

## Troubleshooting

<!-- Agents: add troubleshooting entries here as you encounter common project structure and build issues.
     Format: ### Problem title
             **Symptom**: what the developer sees
             **Cause**: why it happens
             **Fix**: how to resolve it -->

### "Ambiguous project name" NuGet restore error

**Symptom**: `dotnet restore` fails with "Ambiguous project name 'MyCompany.MyActivities'" when restoring the packaging project.
**Cause**: Both the library project and the packaging project have the same `<PackageId>`. NuGet can't distinguish them.
**Fix**: Set `<PackageId>MyCompany.MyActivities.Library</PackageId>` and `<IsPackable>false</IsPackable>` in the library `.csproj`. Only the packaging project should own the publishable PackageId.

### Resources class not found / wrong namespace

**Symptom**: `Resources.MyActivity_DisplayName` doesn't compile, or the `Resources` class is in a different namespace than expected.
**Cause**: MSBuild derives the embedded resource manifest name from the project filename, which may differ from the desired namespace. Also, `PublicResXFileCodeGenerator` only runs in Visual Studio — CLI builds need a pre-committed `Resources.Designer.cs`.
**Fix**: Add `<RootNamespace>MyCompany.MyActivities</RootNamespace>` to the library `.csproj`. Ensure `CustomToolNamespace` in the `.csproj` matches this namespace. Generate and commit `Resources.Designer.cs` for CLI builds.

### System.Activities.ViewModels missing types (PersistValuesChangedDuringInit, DefaultWidget)

**Symptom**: Compile errors for `PersistValuesChangedDuringInit`, `DefaultWidget`, or `ViewModelWidgetType` — these types don't exist.
**Cause**: The project references `System.Activities.ViewModels` version `1.0.0-*`, which resolves to a prerelease alpha build missing these APIs.
**Fix**: Pin to the stable release: `Version="1.20251103.2"`. The stable line follows the `1.YYYYMMDD.patch` scheme.
