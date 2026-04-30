# Complete Example: Email Sender Activity Package

> **When to read this:** You are building a new UiPath activity package from scratch and need a full,
> copy-paste-ready reference covering every file -- activity, ViewModel, metadata, resources, tests,
> project files, and build commands. Start here, then consult the individual guides for deeper
> explanation of each layer.

## Cross-references

- Activity class patterns: [../core/activity-classes.md](../core/activity-classes.md)
- ViewModel design: [../design/viewmodel-basics.md](../design/viewmodel-basics.md)
- Widget reference: [../design/widgets-and-datasources.md](../design/widgets-and-datasources.md)
- Testing patterns: [../testing/testing-guide.md](../testing/testing-guide.md)
- Service access table: [../reference/service-access.md](../reference/service-access.md)
- Compilation constants: [../reference/compilation-constants.md](../reference/compilation-constants.md)
- Feature flags: [../reference/feature-flags.md](../reference/feature-flags.md)

---

## Solution layout

```
MyCompany.EmailActivities/
  MyCompany.EmailActivities/
    Activities/EmailSender.cs
    Helpers/ActivityContextExtensions.cs
    ViewModels/EmailSenderViewModel.cs
    Resources/
      ActivitiesMetadata.json
      Icons/email.svg
      Resources.resx
      Resources.Designer.cs        (auto-generated)
    MyCompany.EmailActivities.csproj
  MyCompany.EmailActivities.Packaging/
    MyCompany.EmailActivities.Packaging.csproj
  MyCompany.EmailActivities.Tests/
    EmailSenderUnitTests.cs
    Workflow/EmailSenderWorkflowTests.cs
    MyCompany.EmailActivities.Tests.csproj
  Output/Packages/                 (build output)
  nuget.config
  MyCompany.EmailActivities.sln
```

---

## 1. Activity class

**File:** `MyCompany.EmailActivities/Activities/EmailSender.cs`

```csharp
// Activities/EmailSender.cs
using System.Activities;
using System.ComponentModel;
using System.Diagnostics;
using MyCompany.EmailActivities.Helpers;
using UiPath.Robot.Activities.Api;

namespace MyCompany.EmailActivities;

public class EmailSender : CodeActivity
{
    [RequiredArgument]
    public InArgument<string> To { get; set; }

    [RequiredArgument]
    public InArgument<string> Subject { get; set; }

    [RequiredArgument]
    public InArgument<string> Body { get; set; }

    public InArgument<string> Cc { get; set; }

    public EmailPriority Priority { get; set; } = EmailPriority.Normal;

    public bool IsHtml { get; set; } = false;

    public OutArgument<string> MessageId { get; set; }

    protected override void Execute(CodeActivityContext context)
    {
        context.GetExecutorRuntime().LogMessage(new LogMessage
        {
            EventType = TraceEventType.Information,
            Message = "Executing EmailSender activity"
        });

        var to = To.Get(context);
        var subject = Subject.Get(context);
        var body = Body.Get(context);
        var cc = Cc?.Get(context);
        var messageId = SendEmail(to, subject, body, cc, Priority, IsHtml);
        MessageId.Set(context, messageId);
    }

    public string SendEmail(string to, string subject, string body,
        string cc, EmailPriority priority, bool isHtml)
    {
        // Business logic here
        return Guid.NewGuid().ToString();
    }
}

public enum EmailPriority { Low, Normal, High }
```

Key points:
- Inherits `CodeActivity` for synchronous execution.
- `[RequiredArgument]` marks mandatory inputs.
- `InArgument<T>` for inputs, `OutArgument<T>` for outputs.
- Plain C# properties (`Priority`, `IsHtml`) are persisted as literals, not expressions.
- Business logic (`SendEmail`) is a separate public method for unit-testability.

---

## 2. Helper extension

**File:** `MyCompany.EmailActivities/Helpers/ActivityContextExtensions.cs`

```csharp
// Helpers/ActivityContextExtensions.cs
using System.Activities;
using UiPath.Robot.Activities.Api;

namespace MyCompany.EmailActivities.Helpers;

public static class ActivityContextExtensions
{
    public static IExecutorRuntime GetExecutorRuntime(this ActivityContext context)
        => context.GetExtension<IExecutorRuntime>();
}
```

This avoids scattering `context.GetExtension<IExecutorRuntime>()` calls throughout activity code.

---

## 3. ViewModel class

**File:** `MyCompany.EmailActivities/ViewModels/EmailSenderViewModel.cs`

```csharp
// ViewModels/EmailSenderViewModel.cs
using System.Activities.ViewModels;

namespace MyCompany.EmailActivities.ViewModels;

public class EmailSenderViewModel : DesignPropertiesViewModel
{
    // Property names match Activity properties exactly
    public DesignInArgument<string> To { get; set; }
    public DesignInArgument<string> Subject { get; set; }
    public DesignInArgument<string> Body { get; set; }
    public DesignInArgument<string> Cc { get; set; }
    public DesignProperty<EmailPriority> Priority { get; set; }
    public DesignProperty<bool> IsHtml { get; set; }
    public DesignOutArgument<string> MessageId { get; set; }

    public EmailSenderViewModel(IDesignServices services) : base(services) { }

    protected override void InitializeModel()
    {
        base.InitializeModel();
        PersistValuesChangedDuringInit();

        var order = 0;

        // Principal (main) properties
        To.DisplayName = Resources.EmailSender_To_DisplayName;
        To.Tooltip = Resources.EmailSender_To_Tooltip;
        To.IsRequired = true;
        To.IsPrincipal = true;
        To.OrderIndex = order++;

        Subject.DisplayName = Resources.EmailSender_Subject_DisplayName;
        Subject.Tooltip = Resources.EmailSender_Subject_Tooltip;
        Subject.IsRequired = true;
        Subject.IsPrincipal = true;
        Subject.OrderIndex = order++;

        Body.DisplayName = Resources.EmailSender_Body_DisplayName;
        Body.Tooltip = Resources.EmailSender_Body_Tooltip;
        Body.IsRequired = true;
        Body.IsPrincipal = true;
        Body.Widget = new DefaultWidget
        {
            Type = ViewModelWidgetType.TextComposer
        };
        Body.OrderIndex = order++;

        // Secondary properties
        Cc.DisplayName = Resources.EmailSender_Cc_DisplayName;
        Cc.Tooltip = Resources.EmailSender_Cc_Tooltip;
        Cc.OrderIndex = order++;

        Priority.DisplayName = Resources.EmailSender_Priority_DisplayName;
        Priority.Tooltip = Resources.EmailSender_Priority_Tooltip;
        Priority.DataSource = EnumDataSourceBuilder<EmailPriority>
            .Configure()
            .WithSingleItemConverter(
                itemToValue: item => item.ToString(),
                valueToItem: value => Enum.TryParse<EmailPriority>(value, out var v)
                    ? v : EmailPriority.Normal)
            .WithData(Enum.GetValues<EmailPriority>())
            .Build();
        Priority.OrderIndex = order++;

        IsHtml.DisplayName = Resources.EmailSender_IsHtml_DisplayName;
        IsHtml.Tooltip = Resources.EmailSender_IsHtml_Tooltip;
        IsHtml.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };
        IsHtml.OrderIndex = order++;

        // Output
        MessageId.DisplayName = Resources.EmailSender_MessageId_DisplayName;
        MessageId.Tooltip = Resources.EmailSender_MessageId_Tooltip;
        MessageId.OrderIndex = order++;
    }

    protected override void InitializeRules()
    {
        base.InitializeRules();

        // When IsHtml changes, update Body widget
        Rule(nameof(IsHtml), () =>
        {
            Body.Widget = IsHtml.Value
                ? new DefaultWidget { Type = ViewModelWidgetType.RichTextComposer }
                : new DefaultWidget { Type = ViewModelWidgetType.TextComposer };
        });
    }

    protected override void ManualRegisterDependencies()
    {
        base.ManualRegisterDependencies();
        RegisterDependency(IsHtml, nameof(IsHtml.Value), nameof(IsHtml));
    }
}
```

Key points:
- Property names on the ViewModel must match the Activity class exactly.
- `IsPrincipal = true` surfaces properties in the collapsed card view.
- `OrderIndex` controls display order in the properties panel.
- `InitializeRules()` defines reactive rules (e.g., toggling the body widget when `IsHtml` changes).
- `ManualRegisterDependencies()` wires up property-change tracking for rules.

---

## 4. Metadata JSON

**File:** `MyCompany.EmailActivities/Resources/ActivitiesMetadata.json`

```json
{
  "resourceManagerName": "MyCompany.EmailActivities.Resources",
  "activities": [
    {
      "fullName": "MyCompany.EmailActivities.EmailSender",
      "shortName": "EmailSender",
      "displayNameKey": "EmailSender_DisplayName",
      "descriptionKey": "EmailSender_Description",
      "categoryKey": "Email",
      "iconKey": "email.svg",
      "viewModelType": "MyCompany.EmailActivities.ViewModels.EmailSenderViewModel"
    }
  ]
}
```

This file must be embedded as a resource in the `.csproj`. The `resourceManagerName` points to the
generated `Resources.Designer.cs` class. Each entry maps an activity to its display name, icon, and
ViewModel.

---

## 5. Resources

**File:** `MyCompany.EmailActivities/Resources/Resources.resx`

```xml
<!-- Resources.resx -->
<data name="EmailSender_DisplayName"><value>Send Email</value></data>
<data name="EmailSender_Description"><value>Sends an email message</value></data>
<data name="EmailSender_To_DisplayName"><value>To</value></data>
<data name="EmailSender_To_Tooltip"><value>Recipient email address</value></data>
<data name="EmailSender_Subject_DisplayName"><value>Subject</value></data>
<data name="EmailSender_Subject_Tooltip"><value>Email subject line</value></data>
<data name="EmailSender_Body_DisplayName"><value>Body</value></data>
<data name="EmailSender_Body_Tooltip"><value>Email body content</value></data>
<data name="EmailSender_Cc_DisplayName"><value>CC</value></data>
<data name="EmailSender_Cc_Tooltip"><value>Carbon copy recipients (optional)</value></data>
<data name="EmailSender_Priority_DisplayName"><value>Priority</value></data>
<data name="EmailSender_Priority_Tooltip"><value>Email priority level</value></data>
<data name="EmailSender_IsHtml_DisplayName"><value>HTML Body</value></data>
<data name="EmailSender_IsHtml_Tooltip"><value>Whether the body contains HTML</value></data>
<data name="EmailSender_MessageId_DisplayName"><value>Message ID</value></data>
<data name="EmailSender_MessageId_Tooltip"><value>Unique ID of the sent message</value></data>
```

Naming convention: `{ActivityShortName}_{PropertyName}_{DisplayName|Tooltip|Description}`.

---

## 6. Unit test

**File:** `MyCompany.EmailActivities.Tests/EmailSenderUnitTests.cs`

```csharp
public class EmailSenderUnitTests
{
    [Fact]
    public void SendEmail_ReturnsMessageId()
    {
        var sender = new EmailSender();
        var result = sender.SendEmail("test@example.com", "Test", "Body",
            null, EmailPriority.Normal, false);
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }
}
```

Unit tests exercise the public business-logic method directly, without the workflow runtime.

---

## 7. Workflow test

**File:** `MyCompany.EmailActivities.Tests/Workflow/EmailSenderWorkflowTests.cs`

```csharp
// Tests/Workflow/EmailSenderWorkflowTests.cs
public class EmailSenderWorkflowTests
{
    private readonly Mock<IExecutorRuntime> _runtimeMock = new();

    [Fact]
    public void SendEmail_SetsMessageIdOutput()
    {
        var activity = new EmailSender
        {
            To = "test@example.com",
            Subject = "Test",
            Body = "Hello",
            Priority = EmailPriority.High,
            IsHtml = false
        };

        var runner = new WorkflowInvoker(activity);
        runner.Extensions.Add(() => _runtimeMock.Object);

        var result = runner.Invoke(TimeSpan.FromSeconds(5));

        Assert.NotNull(result["MessageId"]);
    }
}
```

Workflow tests run the activity inside a `WorkflowInvoker`, validating that inputs/outputs flow
correctly through the runtime. Mock `IExecutorRuntime` to avoid real robot dependencies.

---

## 8. nuget.config

**File:** `nuget.config` (solution root)

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="UiPath Official" value="https://uipath.pkgs.visualstudio.com/Public.Feeds/_packaging/UiPath-Official/nuget/v3/index.json" />
  </packageSources>
</configuration>
```

The UiPath Official feed provides `UiPath.Activities.Api`, `UiPath.Workflow`, and
`System.Activities.ViewModels`.

---

## 9. Main project .csproj

**File:** `MyCompany.EmailActivities/MyCompany.EmailActivities.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <PackageId>MyCompany.EmailActivities</PackageId>
  </PropertyGroup>

  <ItemGroup>
    <None Remove="Resources\ActivitiesMetadata.json" />
    <EmbeddedResource Include="Resources\ActivitiesMetadata.json" />
    <EmbeddedResource Include="Resources\Icons\email.svg" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="System.Activities.ViewModels" Version="1.0.0-*" />
    <PackageReference Include="UiPath.Activities.Api" Version="24.10.1" PrivateAssets="All" />
    <PackageReference Include="UiPath.Workflow" Version="6.0.0-*" PrivateAssets="All" />
  </ItemGroup>

  <ItemGroup>
    <Compile Update="Resources\Resources.Designer.cs">
      <DependentUpon>Resources.resx</DependentUpon>
      <DesignTime>True</DesignTime>
      <AutoGen>True</AutoGen>
      <CustomToolNamespace>MyCompany.EmailActivities</CustomToolNamespace>
    </Compile>
  </ItemGroup>
  <ItemGroup>
    <EmbeddedResource Update="Resources\Resources.resx">
      <LastGenOutput>Resources.Designer.cs</LastGenOutput>
      <Generator>PublicResXFileCodeGenerator</Generator>
      <CustomToolNamespace>MyCompany.EmailActivities</CustomToolNamespace>
    </EmbeddedResource>
  </ItemGroup>
</Project>
```

Key points:
- `ActivitiesMetadata.json` and SVG icons are embedded resources.
- `PrivateAssets="All"` on UiPath packages prevents them from flowing to consumers (the robot provides them at runtime).
- `PublicResXFileCodeGenerator` auto-generates the `Resources.Designer.cs` typed accessor class.

---

## 10. Packaging project .csproj

**File:** `MyCompany.EmailActivities.Packaging/MyCompany.EmailActivities.Packaging.csproj`

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
    <PackageId>MyCompany.EmailActivities</PackageId>
    <Authors>MyCompany</Authors>
    <Description>Email sending activities for UiPath</Description>
    <PackageTags>UiPathActivities</PackageTags>
    <PackageOutputPath>..\Output\Packages\</PackageOutputPath>
    <TargetsForTfmSpecificBuildOutput>AddDlls</TargetsForTfmSpecificBuildOutput>
    <ProduceReferenceAssembly>False</ProduceReferenceAssembly>
  </PropertyGroup>
  <Target Name="AddDlls">
    <ItemGroup Condition="'$(Configuration)' == 'Debug'">
      <BuildOutputInPackage Include="$(OutputPath)MyCompany.EmailActivities.pdb" />
    </ItemGroup>
    <ItemGroup>
      <BuildOutputInPackage Include="$(OutputPath)MyCompany.EmailActivities.dll" />
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
    <ProjectReference Include="..\MyCompany.EmailActivities\MyCompany.EmailActivities.csproj">
      <PrivateAssets>All</PrivateAssets>
    </ProjectReference>
  </ItemGroup>
</Project>
```

Key points:
- Separate packaging project keeps the main project clean.
- `GeneratePackageOnBuild` produces a `.nupkg` on every build.
- Debug builds get a timestamped prerelease version (e.g., `1.0.61-dev.832`).
- Release builds get a clean semver (e.g., `1.0.0`).
- `AddDlls` target manually includes the main project DLL (and PDB in debug) into the package.
- `CleanPackageFiles` deletes old `.nupkg` files before each build.

---

## 11. Test project .csproj

**File:** `MyCompany.EmailActivities.Tests/MyCompany.EmailActivities.Tests.csproj`

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
    <ProjectReference Include="..\MyCompany.EmailActivities\MyCompany.EmailActivities.csproj" />
  </ItemGroup>
</Project>
```

Key points:
- `IsPackable=false` prevents the test project from being packed.
- xunit + Moq is the standard test stack.
- `UiPath.Activities.Api` is marked `DevelopmentDependency="true"` since it is only needed for mocking.

---

## 12. Build and deploy commands

```bash
# Create solution
dotnet new sln -n MyCompany.EmailActivities
dotnet sln add MyCompany.EmailActivities/MyCompany.EmailActivities.csproj
dotnet sln add MyCompany.EmailActivities.Packaging/MyCompany.EmailActivities.Packaging.csproj
dotnet sln add MyCompany.EmailActivities.Tests/MyCompany.EmailActivities.Tests.csproj

# Build (generates Resources.Designer.cs, compiles, produces .nupkg)
dotnet build -c Release

# Run tests
dotnet test

# The .nupkg is at Output/Packages/MyCompany.EmailActivities.1.0.0.nupkg
# Install in Studio: Manage Packages -> Settings -> add local feed -> select Output/Packages/
```

### Build sequence explanation

1. `dotnet build -c Release` triggers the following:
   - `Resources.Designer.cs` is regenerated from `Resources.resx`.
   - The main project compiles, embedding `ActivitiesMetadata.json` and icons.
   - The packaging project compiles, pulling the main DLL and producing a `.nupkg`.
2. `dotnet test` discovers and runs all xunit tests in the test project.
3. The output `.nupkg` can be consumed by UiPath Studio via a local NuGet feed.
