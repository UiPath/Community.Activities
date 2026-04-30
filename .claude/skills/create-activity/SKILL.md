---
name: create-activity
description: >
  Generate complete, deployable UiPath activity packages. Detects SDK presence
  and produces all necessary files (activity, viewmodel, metadata, packaging,
  tests). Use when asked to create or scaffold UiPath activities.
allowed-tools: Bash, Glob, Grep, Read, Write, Edit, AskUserQuestion
---

# UiPath Activity Development — Claude Code Skill

> This skill file enables Claude Code to generate complete, deployable UiPath activity packages.
> For detailed reference on widgets, data sources, rules, bindings, advanced patterns, and testing,
> see the companion guide: [`activity-development-guide/index.md`](../../activity-development-guide/index.md).

---

## When to Use This Skill

Use this skill when asked to:
- Create a new UiPath activity (or activity package)
- Add a new activity to an existing UiPath activity package
- Scaffold the project structure for UiPath activities

---

## Before You Generate — Resolve Package Name

Before generating any file, determine `{PackageName}` and `{Namespace}`:

**Step 1 — Infer from the prompt.**
If the user's request contains a dotted name (e.g. "AlexPetre.ConvertXml"), extract
the full dotted name as `{PackageName}` and use it as `{Namespace}` too.

| User writes | {PackageName} | {Namespace} |
|-------------|---------------|-------------|
| "AlexPetre.ConvertXml activity" | `AlexPetre.ConvertXml` | `AlexPetre.ConvertXml` |
| "create Acme.Utils.Csv activity" | `Acme.Utils.Csv` | `Acme.Utils.Csv` |
| "ConvertXml activity" (no dots) | ask — see Step 2 | ask — see Step 2 |

**Step 2 — Ask if no dotted name is found.**
If the prompt contains no dotted name, stop and ask:

> What should the package be called? (e.g. YourName.ConvertXml)

Use the user's full answer as `{PackageName}` and `{Namespace}`.

**Step 3 — Normalize to PascalCase.**
If the name is not already PascalCase, convert it: capitalize the first letter of each word and remove spaces/hyphens.

**Rule: `{PackageName}` MUST NOT start with `UiPath`.** Never generate a package whose name begins with `UiPath`.

---

## SDK Detection

**Before generating any files**, check if the UiPath Activities SDK is present in the repository:

```
Glob for **/UiPath.Sdk.Activities/*.cs  or  **/UiPath.Sdk.Activities.projitems
```

- **If found -> SDK mode** (preferred): Use the SDK base classes and patterns described in the **SDK Mode Overrides** section below. The SDK provides dependency injection, telemetry, governance, project settings, retry, and connection binding support out of the box.
- **If not found -> Classic mode**: Use the standard templates below.

---

## File Generation Checklist

### Classic Mode

When creating a **new activity package from scratch**, generate ALL of these files in order:

| # | File | Purpose |
|---|------|---------|
| 1 | `nuget.config` | NuGet feed configuration |
| 2 | `{PackageName}/{PackageName}.csproj` | Main library project |
| 3 | `{PackageName}/Activities/{ActivityName}.cs` | Runtime activity class |
| 4 | `{PackageName}/ViewModels/{ActivityName}ViewModel.cs` | Design-time ViewModel |
| 5 | `{PackageName}/Helpers/ActivityContextExtensions.cs` | Runtime logging helper |
| 6 | `{PackageName}/Resources/ActivitiesMetadata.json` | Activity discovery metadata |
| 6b | `{PackageName}/Resources/Icons/activityicon.svg` | Default activity icon (SVG 24x24) |
| 7 | `{PackageName}/Resources/Resources.resx` | Localized display strings |
| 7b | `{PackageName}/Resources/Resources.Designer.cs` | Strongly-typed resource accessor |
| 8 | `{PackageName}.Packaging/{PackageName}.Packaging.csproj` | NuGet packaging project |
| 9 | `{PackageName}.Tests/{PackageName}.Tests.csproj` | Test project |
| 10 | `{PackageName}.Tests/Unit/{ActivityName}UnitTests.cs` | Unit tests |
| 11 | `{PackageName}.Tests/Workflow/{ActivityName}WorkflowTests.cs` | Workflow integration tests |

Then run these commands to create the solution and build:

```bash
dotnet new sln -n {PackageName}
dotnet sln add {PackageName}/{PackageName}.csproj
dotnet sln add {PackageName}.Packaging/{PackageName}.Packaging.csproj
dotnet sln add {PackageName}.Tests/{PackageName}.Tests.csproj
dotnet build -c Release
dotnet test
```

When **adding an activity to an existing package**, generate only files 3, 4, 10, 11 and update files 6, 7, and 7b.

### SDK Mode

When the SDK is detected, the checklist changes. Differences from classic mode are marked with **[SDK]**.

| # | File | Purpose |
|---|------|---------|
| 1 | `nuget.config` | NuGet feed configuration (same as classic) |
| 2 | `{PackageName}/{PackageName}.csproj` | Main library project **[SDK]** — extra properties & packages |
| 3 | `{PackageName}/Activities/{ActivityName}.cs` | Runtime activity class **[SDK]** — `partial`, inherits `SdkActivity<T>` |
| 3b | `{PackageName}/ViewModels/{ActivityName}.Design.cs` | **[SDK NEW]** — `partial` class with `[ViewModelClass]` attribute |
| 4 | `{PackageName}/ViewModels/{ActivityName}ViewModel.cs` | Design-time ViewModel **[SDK]** — inherits `BaseViewModel` |
| -- | ~~`ActivityContextExtensions.cs`~~ | **[SDK]** — NOT needed (services via `RuntimeServices`) |
| 5 | `{PackageName}/Directory.build.targets` | **[SDK NEW]** — imports SDK build targets |
| 6 | `{PackageName}/Resources/ActivitiesMetadata.json` | Activity discovery metadata (same as classic) |
| 6b | `{PackageName}/Resources/Icons/activityicon.svg` | Default activity icon (same as classic) |
| 7 | `{PackageName}/Resources/Resources.resx` | Localized display strings (same as classic) |
| 7b | `{PackageName}/Resources/Resources.Designer.cs` | Strongly-typed resource accessor (same as classic) |
| 8 | `{PackageName}.Packaging/{PackageName}.Packaging.csproj` | NuGet packaging project (same as classic) |
| 9 | `{PackageName}.Tests/{PackageName}.Tests.csproj` | Test project **[SDK]** — extra properties |
| 10 | `{PackageName}.Tests/Unit/{ActivityName}UnitTests.cs` | Unit tests **[SDK]** — DI-based testing |
| 11 | `{PackageName}.Tests/Workflow/{ActivityName}WorkflowTests.cs` | Workflow integration tests (same as classic) |

When **adding an activity to an existing SDK package**, generate files 3, 3b, 4, 10, 11 and update files 6, 7, and 7b.

### Common Notes

**`Resources.Designer.cs` — generate and commit for CLI builds:** `PublicResXFileCodeGenerator` only runs inside Visual Studio. For `dotnet build` and CI builds, `Resources.Designer.cs` must be generated and committed to source (see Template 7b). Visual Studio will regenerate it automatically when the project is opened; that is expected and harmless.

For more details, see [`core/project-structure.md`](../../activity-development-guide/core/project-structure.md).

---

## File Templates

### 1. nuget.config

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="UiPath Official" value="https://uipath.pkgs.visualstudio.com/Public.Feeds/_packaging/UiPath-Official/nuget/v3/index.json" />
  </packageSources>
</configuration>
```

### 2. Main Project .csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <!-- Library gets distinct PackageId to avoid "Ambiguous project name" with Packaging project -->
    <PackageId>{PackageName}.Library</PackageId>
    <IsPackable>false</IsPackable>
    <!-- Explicit RootNamespace ensures embedded resource manifest name is {PackageName}.Resources.Resources -->
    <RootNamespace>{PackageName}</RootNamespace>
    <NoWarn>$(NoWarn);NU5104</NoWarn>
  </PropertyGroup>

  <ItemGroup>
    <None Remove="Resources\ActivitiesMetadata.json" />
    <EmbeddedResource Include="Resources\ActivitiesMetadata.json" />
    <EmbeddedResource Include="Resources\Icons\*.svg" />
  </ItemGroup>

  <ItemGroup>
    <!-- Pin to stable release (1.YYYYMMDD.patch). Never use 1.0.0-* — resolves to alpha missing key APIs. -->
    <PackageReference Include="System.Activities.ViewModels" Version="1.20251103.2" PrivateAssets="All" />
    <PackageReference Include="UiPath.Activities.Api" Version="24.10.1" PrivateAssets="All" />
    <PackageReference Include="UiPath.Workflow" Version="6.0.0-*" PrivateAssets="All" />
  </ItemGroup>

  <ItemGroup>
    <Compile Update="Resources\Resources.Designer.cs">
      <DependentUpon>Resources.resx</DependentUpon>
      <DesignTime>True</DesignTime>
      <AutoGen>True</AutoGen>
      <CustomToolNamespace>{Namespace}</CustomToolNamespace>
    </Compile>
  </ItemGroup>
  <ItemGroup>
    <EmbeddedResource Update="Resources\Resources.resx">
      <LastGenOutput>Resources.Designer.cs</LastGenOutput>
      <Generator>PublicResXFileCodeGenerator</Generator>
      <CustomToolNamespace>{Namespace}</CustomToolNamespace>
    </EmbeddedResource>
  </ItemGroup>
</Project>
```

### 3. Activity Class

```csharp
// Activities/{ActivityName}.cs
using System.Activities;
using System.ComponentModel;
using System.Diagnostics;
using {Namespace}.Helpers;
using UiPath.Robot.Activities.Api;

namespace {Namespace};

public class {ActivityName} : CodeActivity{<ResultType> if applicable}
{
    [RequiredArgument]
    public InArgument<T>? {InputProp} { get; set; }

    public OutArgument<T>? {OutputProp} { get; set; }

    // Enum selector: plain TEnum, NOT InArgument<TEnum> — see activity-code.md
    public TEnum {EnumProp} { get; set; } = TEnum.DefaultValue;

    protected override {ReturnType} Execute(CodeActivityContext context)
    {
        context.GetExecutorRuntime().LogMessage(new LogMessage
        {
            EventType = TraceEventType.Information,
            Message = "Executing {ActivityName} activity"
        });

        var input = {InputProp}.Get(context);
        var result = ExecuteInternal(input);
        {OutputProp}?.Set(context, result);
        return result; // if CodeActivity<T>
    }

    // ExecuteInternal reads enum property directly — not via parameter or private field.
    // See activity-development-guide/runtime/activity-code.md for the ExecuteInternal pattern.
    public {ReturnType} ExecuteInternal({params})
    {
        return {EnumProp} switch
        {
            // Business logic here
        };
    }
}
```

**Classic mode base class selection** (for SDK mode, see SDK Mode Overrides section):

| Base Class | When to Use |
|---|---|
| `CodeActivity<T>` | Simple synchronous activities returning a value |
| `CodeActivity` | Simple synchronous activities with no return value |
| `AsyncCodeActivity<T>` | Async activities (I/O, network calls) |

**Property type mapping:**

| C# Type | Purpose |
|---|---|
| `InArgument<T>?` | Input — accepts expressions/variables |
| `OutArgument<T>?` | Output — writes to variables |
| `InOutArgument<T>?` | Bidirectional — read and modify |
| `TEnum` (plain type) | Enum selector — maps to `DesignProperty<TEnum>` in ViewModel |
| `T` (direct type) | Other constants (bool, int, etc.) |

For details on enum selectors, the ExecuteInternal pattern, and GetExecutorRuntime, see [`runtime/activity-code.md`](../../activity-development-guide/runtime/activity-code.md).

### 4. ViewModel Class

Property names **MUST exactly match** the Activity class property names.

```csharp
// ViewModels/{ActivityName}ViewModel.cs
using System.Activities.DesignViewModels;

namespace {Namespace}.ViewModels;

public class {ActivityName}ViewModel : DesignPropertiesViewModel
{
    // Nullable without initializer — framework populates before InitializeModel
    public DesignInArgument<T>? {InputProp} { get; set; }
    public DesignOutArgument<T>? {OutputProp} { get; set; }
    public DesignProperty<TEnum>? {EnumProp} { get; set; }

    public {ActivityName}ViewModel(IDesignServices services) : base(services) { }

    protected override void InitializeModel()
    {
        base.InitializeModel();
        PersistValuesChangedDuringInit();

        var order = 0;

        // Use ! on every nullable property access — framework guarantees non-null here
        {InputProp}!.DisplayName = Resources.{ActivityName}_{InputProp}_DisplayName;
        {InputProp}!.Tooltip = Resources.{ActivityName}_{InputProp}_Tooltip;
        {InputProp}!.IsRequired = true;
        {InputProp}!.IsPrincipal = true;
        {InputProp}!.OrderIndex = order++;

        // Enum properties — runtime auto-renders enums as dropdowns
        {EnumProp}!.DisplayName = Resources.{ActivityName}_{EnumProp}_DisplayName;
        {EnumProp}!.IsPrincipal = true;
        {EnumProp}!.OrderIndex = order++;

        // Output properties — not principal, at the end
        {OutputProp}!.DisplayName = Resources.{ActivityName}_{OutputProp}_DisplayName;
        {OutputProp}!.Tooltip = Resources.{ActivityName}_{OutputProp}_Tooltip;
        {OutputProp}!.OrderIndex = order++;
    }
}
```

**ViewModel property type mapping:**

| ViewModel Type | Maps to Activity Type | `.Widget` supported |
|---|---|---|
| `DesignInArgument<T>` | `InArgument<T>` | No |
| `DesignOutArgument<T>` | `OutArgument<T>` | No |
| `DesignInOutArgument<T>` | `InOutArgument<T>` | No |
| `DesignProperty<T>` | Direct `T` property | **Yes** |

**Common widget assignments** (set in `InitializeModel()`):

```csharp
// Toggle for booleans (only on DesignProperty, not DesignInArgument)
BoolProp!.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };

// Multi-line text
TextProp!.Widget = new DefaultWidget
{
    Type = ViewModelWidgetType.TextComposer,
    Metadata = new() { { TextComposerMetadata.IsSingleLineFormat, true.ToString() } }
};

// Autocomplete dropdown (searchable, allows expressions)
SearchProp!.Widget = new DefaultWidget { Type = ViewModelWidgetType.AutoCompleteForExpression };

// Plain number with constraints
NumProp!.Widget = new DefaultWidget
{
    Type = ViewModelWidgetType.PlainNumber,
    Metadata = new() { [PlainNumber.Min] = "0", [PlainNumber.Max] = "100", [PlainNumber.Step] = "1" }
};
```

For nullable properties, the `!` operator, rules, dependencies, data sources, menu actions, validation, and advanced patterns, see:
- [ViewModel Guide](../../activity-development-guide/design/viewmodel.md) — nullable properties, InitializeModel patterns, resource cleanup
- [Rules and Dependencies](../../activity-development-guide/design/rules-and-dependencies.md)
- [DataSource Patterns](../../activity-development-guide/design/datasources.md)
- [Menu Actions](../../activity-development-guide/design/menu-actions.md)
- [Validation](../../activity-development-guide/design/validation.md)
- [Bindings](../../activity-development-guide/design/bindings.md)
- [Widgets](../../activity-development-guide/design/widgets/index.md)
- [Advanced Patterns](../../activity-development-guide/advanced/patterns.md)

### 5. Helper Extension

```csharp
// Helpers/ActivityContextExtensions.cs
using System.Activities;
using UiPath.Robot.Activities.Api;

namespace {Namespace}.Helpers;

public static class ActivityContextExtensions
{
    public static IExecutorRuntime GetExecutorRuntime(this ActivityContext context)
        => context.GetExtension<IExecutorRuntime>();
}
```

### 6. ActivitiesMetadata.json

```json
{
  "resourceManagerName": "{PackageName}.Resources.Resources",
  "activities": [
    {
      "fullName": "{Namespace}.{ActivityName}",
      "shortName": "{ActivityName}",
      "displayNameKey": "{ActivityName}_DisplayName",
      "descriptionKey": "{ActivityName}_Description",
      "categoryKey": "{Category}",
      "iconKey": "activityicon.svg",
      "viewModelType": "{Namespace}.ViewModels.{ActivityName}ViewModel"
    }
  ]
}
```

When adding multiple activities, add entries to the `activities` array.

### 6b. activityicon.svg

Default activity icon. Place at `{PackageName}/Resources/Icons/activityicon.svg`.

```xml
<svg width="24" height="24" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
	<path d="M5.00671 19V14H7.00671V19H18V15.5H16L19 12.5L22 15.5H20V19C20 20.1 19.1 21 18 21H10.5H8.00671H7.00671C5.90214 21 5.00671 20.1046 5.00671 19Z" fill="#556068"/>
	<path fill-rule="evenodd" clip-rule="evenodd" d="M8.11702 2.87616L12 5.46482V9.67703L5.86054 12.1328L2 9.3446V5.32297L8.11702 2.87616ZM4 6.67703V8.32198L6.13946 9.86718L10 8.32297V6.53518L7.88298 5.12384L4 6.67703Z" fill="#556068"/>
	<path d="M16 7.5C16 9.15685 17.3431 10.5 19 10.5C20.6569 10.5 22 9.15685 22 7.5C22 5.84315 20.6569 4.5 19 4.5C17.3431 4.5 16 5.84315 16 7.5Z" fill="#556068"/>
</svg>
```

All activities in the package share this icon. For per-activity icons, add SVG files to `Resources/Icons/` and update `iconKey` in `ActivitiesMetadata.json` — the glob `<EmbeddedResource Include="Resources\Icons\*.svg" />` picks them up automatically.

### 7. Resources.resx

```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <xsd:schema id="root" xmlns="" xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:msdata="urn:schemas-microsoft-com:xml-msdata">
    <xsd:element name="root" msdata:IsDataSet="true">
      <xsd:complexType>
        <xsd:choice maxOccurs="unbounded">
          <xsd:element name="data">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name="value" type="xsd:string" minOccurs="0" msdata:Ordinal="1" />
                <xsd:element name="comment" type="xsd:string" minOccurs="0" msdata:Ordinal="2" />
              </xsd:sequence>
              <xsd:attribute name="name" type="xsd:string" msdata:Ordinal="0" />
              <xsd:attribute name="type" type="xsd:string" />
              <xsd:attribute name="mimetype" type="xsd:string" />
              <xsd:attribute ref="xml:space" />
            </xsd:complexType>
          </xsd:element>
          <xsd:element name="resheader">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name="value" type="xsd:string" minOccurs="0" msdata:Ordinal="1" />
              </xsd:sequence>
              <xsd:attribute name="name" type="xsd:string" use="required" />
            </xsd:complexType>
          </xsd:element>
        </xsd:choice>
      </xsd:complexType>
    </xsd:element>
  </xsd:schema>
  <resheader name="resmimetype">
    <value>text/microsoft-resx</value>
  </resheader>
  <resheader name="version">
    <value>2.0</value>
  </resheader>
  <resheader name="reader">
    <value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <resheader name="writer">
    <value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <!-- Activity display name and description -->
  <data name="{ActivityName}_DisplayName" xml:space="preserve">
    <value>{Activity Display Name}</value>
  </data>
  <data name="{ActivityName}_Description" xml:space="preserve">
    <value>{Activity description text}</value>
  </data>
  <!-- Property display names and tooltips: {ActivityName}_{PropertyName}_DisplayName / _Tooltip -->
  <data name="{ActivityName}_{PropertyName}_DisplayName" xml:space="preserve">
    <value>{Property Display Name}</value>
  </data>
  <data name="{ActivityName}_{PropertyName}_Tooltip" xml:space="preserve">
    <value>{Property tooltip/help text}</value>
  </data>
</root>
```

**Naming convention:**
- `{ActivityName}_DisplayName` — Activity display name
- `{ActivityName}_Description` — Activity description
- `{ActivityName}_{PropertyName}_DisplayName` — Property display name
- `{ActivityName}_{PropertyName}_Tooltip` — Property tooltip

### 7b. Resources.Designer.cs

Generate this file manually and commit it. Add one `public static string` property per key defined in `Resources.resx`.

```csharp
// Resources/Resources.Designer.cs
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace {Namespace} {

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class Resources {

        private static System.Resources.ResourceManager resourceMan;

        private static System.Globalization.CultureInfo resourceCulture;

        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        internal Resources() {
        }

        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        public static System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    System.Resources.ResourceManager temp = new System.Resources.ResourceManager("{PackageName}.Resources.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }

        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        public static System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }

        public static string {ActivityName}_DisplayName {
            get {
                return ResourceManager.GetString("{ActivityName}_DisplayName", resourceCulture);
            }
        }

        public static string {ActivityName}_Description {
            get {
                return ResourceManager.GetString("{ActivityName}_Description", resourceCulture);
            }
        }

        public static string {ActivityName}_{PropertyName}_DisplayName {
            get {
                return ResourceManager.GetString("{ActivityName}_{PropertyName}_DisplayName", resourceCulture);
            }
        }

        public static string {ActivityName}_{PropertyName}_Tooltip {
            get {
                return ResourceManager.GetString("{ActivityName}_{PropertyName}_Tooltip", resourceCulture);
            }
        }
    }
}
```

The `ResourceManager` string `"{PackageName}.Resources.Resources"` must match the embedded resource manifest name. This is ensured by setting `<RootNamespace>{PackageName}</RootNamespace>` in the `.csproj`.

### 8. Packaging Project .csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <PropertyGroup>
    <GeneratePackageOnBuild>True</GeneratePackageOnBuild>
    <VersionBuild>$([System.DateTime]::UtcNow.DayOfYear.ToString("F0"))</VersionBuild>
    <VersionRevision>$([System.DateTime]::UtcNow.TimeOfDay.TotalMinutes.ToString("F0"))</VersionRevision>
    <VersionPrefix Condition="'$(Configuration)' == 'Release'">1.0.0</VersionPrefix>
    <VersionPrefix Condition="'$(Configuration)' == 'Debug'">1.0.$(VersionBuild)-dev.$(VersionRevision)</VersionPrefix>
    <PackageId>{PackageName}</PackageId>
    <Authors>{AuthorName}</Authors>
    <Description>{Package description}</Description>
    <PackageTags>UiPathActivities</PackageTags>
    <PackageOutputPath>..\Output\Packages\</PackageOutputPath>
    <TargetsForTfmSpecificBuildOutput>AddDlls</TargetsForTfmSpecificBuildOutput>
    <ProduceReferenceAssembly>False</ProduceReferenceAssembly>
  </PropertyGroup>
  <Target Name="AddDlls">
    <ItemGroup Condition="'$(Configuration)' == 'Debug'">
      <BuildOutputInPackage Include="$(OutputPath){PackageName}.pdb" />
    </ItemGroup>
    <ItemGroup>
      <BuildOutputInPackage Include="$(OutputPath){PackageName}.dll" />
    </ItemGroup>
  </Target>
  <Target Name="RemoveMetaDll" AfterTargets="BuiltProjectOutputGroup">
    <ItemGroup>
      <BuiltProjectOutputGroupOutput Remove="@(BuiltProjectOutputGroupOutput)" />
    </ItemGroup>
  </Target>
  <Target Name="CleanPackageFiles" BeforeTargets="Build">
    <ItemGroup>
      <PackageFilesToDelete Include="$(PackageOutputPath)\$(PackageId)*.nupkg" />
    </ItemGroup>
    <Delete Files="@(PackageFilesToDelete)" ContinueOnError="WarnAndContinue" />
  </Target>
  <ItemGroup>
    <ProjectReference Include="..\{PackageName}\{PackageName}.csproj">
      <PrivateAssets>All</PrivateAssets>
    </ProjectReference>
  </ItemGroup>
</Project>
```

**Critical**: `PackageTags` must contain `UiPathActivities` for Studio to discover the package.

### 9. Test Project .csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="Moq" Version="4.20.72" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="UiPath.Activities.Api" Version="24.10.1" DevelopmentDependency="true" />
    <PackageReference Include="UiPath.Workflow" Version="6.0.0-*" PrivateAssets="All" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\{PackageName}\{PackageName}.csproj" />
  </ItemGroup>
</Project>
```

### 10. Unit Tests

```csharp
// Tests/Unit/{ActivityName}UnitTests.cs
using Xunit;

namespace {Namespace}.Tests.Unit;

public class {ActivityName}UnitTests
{
    [Fact]
    public void ExecuteInternal_ValidInput_ReturnsExpected()
    {
        var activity = new {ActivityName}();
        var result = activity.ExecuteInternal(/* args */);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ExecuteInternal_InvalidInput_Throws()
    {
        var activity = new {ActivityName}();
        Assert.Throws<ExpectedException>(() => activity.ExecuteInternal(/* bad args */));
    }
}
```

### 11. Workflow Tests

```csharp
// Tests/Workflow/{ActivityName}WorkflowTests.cs
using System.Activities;
using Moq;
using UiPath.Robot.Activities.Api;
using Xunit;

namespace {Namespace}.Tests.Workflow;

public class {ActivityName}WorkflowTests
{
    private readonly Mock<IExecutorRuntime> _runtimeMock = new();

    [Fact]
    public void Execute_ValidInputs_ReturnsExpected()
    {
        // Use bare OutArgument — do NOT use Variable<T> (requires enclosing scope).
        // See activity-development-guide/testing/activity-testing.md
        var activity = new {ActivityName}
        {
            {InputProp}  = new InArgument<T>(/* value */),
            {OutputProp} = new OutArgument<T>()
        };

        var runner = new WorkflowInvoker(activity);
        runner.Extensions.Add(() => _runtimeMock.Object);

        var result = runner.Invoke(TimeSpan.FromSeconds(5));

        Assert.True(result.ContainsKey("{OutputProp}"));
        Assert.NotNull(result["{OutputProp}"]);
    }
}
```

---

## SDK Mode Overrides

When in SDK mode, use these templates **instead of** the corresponding classic templates above.
For files not listed here (nuget.config, ActivitiesMetadata.json, Resources.resx, Resources.Designer.cs, Packaging .csproj, activityicon.svg), use the classic templates unchanged.

### SDK: Main Project .csproj (replaces Template 2)

The SDK compiles into your project via shared projects (`.shproj`/`.projitems`). Package versions are centrally managed by the SDK's `Sdk.dependencies.build.targets` — do NOT specify versions for SDK-managed packages.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <PackageId>{PackageName}.Library</PackageId>
    <IsPackable>false</IsPackable>
    <RootNamespace>{PackageName}</RootNamespace>
    <UiPathActivityProject>True</UiPathActivityProject>
    <UiPathActivityDesignProject>True</UiPathActivityDesignProject>
  </PropertyGroup>

  <ItemGroup>
    <None Remove="Resources\ActivitiesMetadata.json" />
    <EmbeddedResource Include="Resources\ActivitiesMetadata.json" />
    <EmbeddedResource Include="Resources\Icons\*.svg" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="UiPath.Activities.Api" />
    <PackageReference Include="UiPath.Activities.Contracts" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" />
    <PackageReference Include="UiPath.Telemetry.Client" />
    <PackageReference Condition=" '$(IsNetCoreUiPath)' == 'true' " Include="System.Activities.ViewModels" />
  </ItemGroup>

  <ItemGroup>
    <Compile Update="Resources\Resources.Designer.cs">
      <DependentUpon>Resources.resx</DependentUpon>
      <DesignTime>True</DesignTime>
      <AutoGen>True</AutoGen>
      <CustomToolNamespace>{Namespace}</CustomToolNamespace>
    </Compile>
  </ItemGroup>
  <ItemGroup>
    <EmbeddedResource Update="Resources\Resources.resx">
      <LastGenOutput>Resources.Designer.cs</LastGenOutput>
      <Generator>PublicResXFileCodeGenerator</Generator>
      <CustomToolNamespace>{Namespace}</CustomToolNamespace>
    </EmbeddedResource>
  </ItemGroup>
</Project>
```

- `UiPathActivityProject=True` -> imports `UiPath.Sdk.Activities.shproj` (runtime SDK code)
- `UiPathActivityDesignProject=True` -> imports `UiPath.Sdk.Activities.Design.shproj` (design-time SDK code)
- Optionally add `<UseIntegrationService>True</UseIntegrationService>` for connection/Integration Service support
- Optionally add `<UseGovernanceService>True</UseGovernanceService>` for governance support

### SDK: Activity Class (replaces Template 3)

The activity is a `partial` class. The other partial contains the `[ViewModelClass]` attribute (see Template 3b).

```csharp
// Activities/{ActivityName}.cs
using System;
using System.Activities;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using UiPath.Robot.Activities.Api;
using UiPath.Sdk.Activities;

namespace {Namespace};

public partial class {ActivityName} : SdkActivity<{ResultType}>
{
    [RequiredArgument]
    public InArgument<T>? {InputProp} { get; set; }

    public {ActivityName}() : base() { }

    /// <summary>
    /// Constructor for unit testing — accepts a pre-configured service provider.
    /// </summary>
    internal {ActivityName}(IServiceProvider provider) : base(provider) { }

    protected override async Task<{ResultType}> ExecuteAsync(
        AsyncCodeActivityContext context,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        // Read ALL inputs before any await — context is disposed after first await
        var input = {InputProp}.Get(context);

        RuntimeServices.ExecutorRuntime?.LogMessage(new LogMessage
        {
            EventType = TraceEventType.Information,
            Message = $"Executing {ActivityName} with input: {input}"
        });

        var result = await DoWorkAsync(input, cancellationToken);

        return result;
    }

    protected override void OnCompleted(AsyncCodeActivityContext context, IServiceProvider serviceProvider)
    {
        base.OnCompleted(context, serviceProvider);
        // Runs after ExecuteAsync completes, with a fresh valid context.
    }
}
```

**SDK base class selection:**

| Base Class | When to Use |
|---|---|
| `SdkActivity<T>` | Async activities returning a value (most common) |
| `SdkNativeActivity<T>` | Activities needing retry, bookmarks, or child activity scheduling |
| `CodeActivity<T>` | Simple synchronous activities (SDK not needed) |
| `CodeActivity` | Simple synchronous with no return value (SDK not needed) |

**Custom service registration** — override `DefaultRuntimeServicePolicy` to register your own services:

```csharp
public class MyServicePolicy : DefaultRuntimeServicePolicy
{
    public override IServicePolicy Register(Action<IServiceCollection> collection = null)
    {
        _services.TryAddSingleton<IMyService, MyService>();
        return base.Register(collection);
    }
}

// Use the custom policy as the second type parameter
public partial class {ActivityName} : SdkActivity<{ResultType}, MyServicePolicy>
```

### SDK: ViewModel Registration (NEW Template 3b)

This file links the activity to its ViewModel via the `[ViewModelClass]` attribute. It is a partial of the activity class.

```csharp
// ViewModels/{ActivityName}.Design.cs
using System.Activities.DesignViewModels;
using {Namespace}.ViewModels;

namespace {Namespace};

[ViewModelClass(typeof({ActivityName}ViewModel))]
public partial class {ActivityName}
{
}
```

### SDK: ViewModel Class (replaces Template 4)

Inherits from the SDK's `BaseViewModel` instead of `DesignPropertiesViewModel`. Uses the built-in `PropertyOrderIndex` counter.

```csharp
// ViewModels/{ActivityName}ViewModel.cs
using System;
using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using UiPath.Sdk.Activities.Design.ViewModels;

namespace {Namespace}.ViewModels;

internal class {ActivityName}ViewModel : BaseViewModel
{
    public DesignInArgument<T>? {InputProp} { get; set; }
    public DesignOutArgument<{ResultType}>? Result { get; set; }
    public DesignProperty<TEnum>? {EnumProp} { get; set; }

    public {ActivityName}ViewModel(IDesignServices services) : base(services) { }

    /// <summary>
    /// Constructor for unit testing — accepts a pre-configured service provider.
    /// </summary>
    internal {ActivityName}ViewModel(IDesignServices services, IServiceProvider activityServices)
        : base(services, activityServices) { }

    protected override void InitializeModel()
    {
        base.InitializeModel();

        {InputProp}!.IsPrincipal = true;
        {InputProp}!.IsRequired = true;
        {InputProp}!.DisplayName = Resources.{ActivityName}_{InputProp}_DisplayName;
        {InputProp}!.Tooltip = Resources.{ActivityName}_{InputProp}_Tooltip;
        {InputProp}!.OrderIndex = PropertyOrderIndex++;

        Result!.DisplayName = Resources.{ActivityName}_Result_DisplayName;
        Result!.OrderIndex = PropertyOrderIndex++;
    }
}
```

**Key differences from classic ViewModel:**

| Classic | SDK |
|---------|-----|
| `DesignPropertiesViewModel` base | `BaseViewModel` base (or `BaseViewModel<TPolicy>`) |
| `var order = 0; prop.OrderIndex = order++;` | `prop.OrderIndex = PropertyOrderIndex++;` (built-in) |
| `public` class | `internal` class (linked via `[ViewModelClass]` attribute) |
| Constructor: `(IDesignServices)` only | Constructor: `(IDesignServices)` + testable `(IDesignServices, IServiceProvider)` |
| No DI in ViewModel | `ActivityServices.GetService<T>()` available for design-time DI |

### SDK: Directory.build.targets (NEW Template 5)

Place this file in the activity pack directory (parent of the `.csproj`). Adjust the relative path to point to the SDK location.

```xml
<Project>
  <Import Project="..\Activities.SDK\Sdk.dependencies.build.targets"/>
  <Import Project="..\Activities.SDK\Sdk.imports.build.targets"/>
</Project>
```

### SDK: Test Project Additions (extends Template 9)

Add these properties to the test `.csproj` to import SDK test infrastructure:

```xml
<PropertyGroup>
  <UiPathActivityTests>true</UiPathActivityTests>
  <UiPathActivityDesignTests>true</UiPathActivityDesignTests>
</PropertyGroup>
```

### SDK: Unit Test Pattern (replaces Template 10)

SDK activities accept an `IServiceProvider` in their constructor, enabling clean DI-based testing:

```csharp
// Tests/Unit/{ActivityName}UnitTests.cs
using System;
using System.Activities;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using UiPath.Robot.Activities.Api;
using Xunit;

namespace {Namespace}.Tests.Unit;

public class {ActivityName}UnitTests
{
    private readonly Mock<IExecutorRuntime> _runtimeMock = new();

    private IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_runtimeMock.Object);
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task ExecuteAsync_ValidInput_ReturnsExpected()
    {
        var provider = BuildServiceProvider();
        var activity = new {ActivityName}(provider)
        {
            {InputProp} = new InArgument<T>(/* value */)
        };

        var runner = new WorkflowInvoker(activity);
        var result = runner.Invoke(TimeSpan.FromSeconds(5));

        Assert.NotNull(result["Result"]);
    }
}
```

---

## Quick Reference: Key Rules

### Both Modes

1. **Property names must match** — ViewModel property names must exactly match Activity property names.
2. **`IsPrincipal = true`** for the 2-4 most important properties (shown in non-collapsible main panel).
3. **`IsRequired = true`** for mandatory properties.
4. **Use `OrderIndex`** with an incrementing counter to control display order.
5. **Localize all strings** via `Resources.resx` — never hardcode display names.
6. **`PackageTags` must contain `UiPathActivities`** for Studio discovery.
7. **`resourceManagerName` in metadata JSON** must be `{PackageName}.Resources.Resources` — set `<RootNamespace>{PackageName}</RootNamespace>` in the `.csproj` to guarantee this.
8. **Generate and commit `Resources.Designer.cs`** — `PublicResXFileCodeGenerator` only runs in Visual Studio; CLI builds need the committed file.
9. **Read all inputs before any `await`** — the `ActivityContext` is disposed after the first await. See [`core/activity-context.md`](../../activity-development-guide/core/activity-context.md).
10. **Never rename or delete activity properties** — existing `.xaml` workflows serialize by name; renaming/removing breaks deserialization. Deprecate with `[Obsolete]` + `[Browsable(false)]` instead.
11. **Enum selectors must be plain `TEnum`** — not `InArgument<TEnum>`. Maps to `DesignProperty<TEnum>` in the ViewModel.
12. **ViewModel namespace is `System.Activities.DesignViewModels`** — NOT `System.Activities.ViewModels`.
13. **ViewModel properties must be nullable** (`DesignInArgument<T>?`) — use `!` on every access in `InitializeModel()`.

### Classic Mode Only

14. **Always call `base.InitializeModel()` then `PersistValuesChangedDuringInit()`** at the start of `InitializeModel()`.
15. **Separate business logic** into `ExecuteInternal()` for testability. See [`runtime/activity-code.md`](../../activity-development-guide/runtime/activity-code.md).

### SDK Mode Only

16. **Activity must be `partial`** — one file for runtime logic, one for `[ViewModelClass]` attribute.
17. **ViewModel is `internal`** — linked to activity via `[ViewModelClass(typeof(...))]`, not via metadata JSON `viewModelType`.
18. **Use `RuntimeServices.ExecutorRuntime`** for logging — no helper extension needed.
19. **Use `PropertyOrderIndex++`** — built into `BaseViewModel`, no manual counter variable needed.
20. **Use constructor DI for testing** — SDK activities accept `IServiceProvider` in an `internal` constructor.

---

## Advanced Features Reference

For these features, consult the corresponding guide file in [`activity-development-guide/`](../../activity-development-guide/index.md):

| Feature | Guide File |
|---------|-----------|
| UiPath platform (Studio, Robot, Orchestrator) | [`core/architecture.md`](../../activity-development-guide/core/architecture.md) |
| ActivityContext lifetime, async patterns | [`core/activity-context.md`](../../activity-development-guide/core/activity-context.md) |
| Project structure, `.csproj` setup, troubleshooting | [`core/project-structure.md`](../../activity-development-guide/core/project-structure.md) |
| Best practices | [`core/best-practices.md`](../../activity-development-guide/core/best-practices.md) |
| Activity code patterns, enum selectors, ExecuteInternal | [`runtime/activity-code.md`](../../activity-development-guide/runtime/activity-code.md) |
| Activities API (runtime/design-time services) | [`runtime/platform-api.md`](../../activity-development-guide/runtime/platform-api.md) |
| Orchestrator integration and version checks | [`runtime/orchestrator.md`](../../activity-development-guide/runtime/orchestrator.md) |
| ViewModel patterns, nullable props, InitializeModel | [`design/viewmodel.md`](../../activity-development-guide/design/viewmodel.md) |
| Widget types and configuration | [`design/widgets/index.md`](../../activity-development-guide/design/widgets/index.md) |
| DataSource patterns (static, dynamic, enum, multi-select) | [`design/datasources.md`](../../activity-development-guide/design/datasources.md) |
| Rules and reactive dependencies | [`design/rules-and-dependencies.md`](../../activity-development-guide/design/rules-and-dependencies.md) |
| Menu actions (buttons, mode switching) | [`design/menu-actions.md`](../../activity-development-guide/design/menu-actions.md) |
| Validation (property-level, model-level, preview) | [`design/validation.md`](../../activity-development-guide/design/validation.md) |
| Metadata schema (full field reference) | [`design/metadata.md`](../../activity-development-guide/design/metadata.md) |
| Orchestrator bindings (ActivitiesBindings.json) | [`design/bindings.md`](../../activity-development-guide/design/bindings.md) |
| Project settings (ArgumentSettingAttribute) | [`design/project-settings.md`](../../activity-development-guide/design/project-settings.md) |
| Solutions vs Project scope (SolutionResourcesWidget) | [`design/solutions.md`](../../activity-development-guide/design/solutions.md) |
| Activity testing (unit + workflow) | [`testing/activity-testing.md`](../../activity-development-guide/testing/activity-testing.md) |
| ViewModel testing approaches | [`testing/viewmodel-testing.md`](../../activity-development-guide/testing/viewmodel-testing.md) |
| Advanced patterns (bidirectional mapping, NativeActivity, bookmarks) | [`advanced/patterns.md`](../../activity-development-guide/advanced/patterns.md) |
| Activities SDK (DI, telemetry, retry, service policies) | [`advanced/sdk-framework.md`](../../activity-development-guide/advanced/sdk-framework.md) |
