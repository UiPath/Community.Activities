---
name: coded-api-doc-generator
description: >
  Generate structured markdown documentation for UiPath coded workflow APIs from source code.
  Produces one doc per package covering service interfaces, methods, return types/outputs, handle
  types, options classes, and copy-paste C# examples. Use when asked to generate coded workflow
  API docs, document coded workflow services, or create API references for coded C# workflows.
  Also use when the user mentions "coded API docs", "service interface docs", or wants to understand
  what methods are available for coded workflows. The XAML activity docs are handled by the separate
  `activity-doc-generator` skill.
---

# Coded Workflow API Doc Generator

Generate structured markdown documentation for UiPath coded workflow service APIs. One file per package, documenting the service interface, methods with parameters and return types, handle types, options classes, enums, and copy-paste C# examples.

## What Coded Workflows Are

UiPath supports two workflow authoring modes:

1. **XAML workflows** — Visual, drag-and-drop activities serialized as Windows Workflow Foundation XAML. Documented by the separate `activity-doc-generator` skill.
2. **Coded workflows** — C# classes that inherit from `CodedWorkflow` and use `[Workflow]`-attributed methods. Instead of configuring activity properties in a designer, developers call service methods in code.

A coded workflow class looks like this:
```csharp
public class MyWorkflow : CodedWorkflow
{
    [Workflow]
    public void Execute()
    {
        // 'excel', 'mail', etc. are service accessors auto-injected by the runtime
        using var workbook = excel.UseExcelFile("data.xlsx");
        var data = workbook.ReadRange("Sheet1");
        Log(data.ToString());
    }
}
```

Each activity package can register a **service interface** (e.g., `IExcelService`) with an accessor variable name (e.g., `excel`) that is automatically available in coded workflow classes. These docs describe that service API — its methods, parameters, return types, and usage patterns.

The coded workflow API is a **fundamentally different surface** from XAML activities. It uses service-oriented patterns with method calls, options objects, and opaque handles — NOT activity classes with properties.

## Core Principles

1. **Service-oriented view** — Document the API as it appears to a coded workflow developer: service accessor, methods, return types.
2. **Copy-paste ready** — C# snippets must compile inside a `[Workflow] public void Execute()` method.
3. **Complete method coverage** — Every public method on service interfaces and handles must be documented.
4. **Return types are outputs** — Method return types and handle types ARE the API's outputs. Document them thoroughly.
5. **Source-of-truth extraction** — All information comes from source code via the extraction script.

---

## Pre-Flight: Does This Domain Have a Coded API?

Not every activity domain exposes a coded workflow API. Before spending time on documentation, check whether there is anything to document.

### Quick Check: Run the Extraction Script

```bash
python {skillPath}/scripts/extract-coded-api.py "{domainRoot}" --pretty --output /tmp/{domain}-coded-api.json
```

Read the JSON output. If `hasCodedApi` is `false`, **stop** — there is no coded API for this domain.

### What Makes a Domain Have a Coded API

A domain has a coded workflow API if and only if it has **both**:

1. **A service registry** — A class implementing `ICodedWorkflowsServiceRegistry` with `[assembly: CodedWorkflowsServiceRegistryAttribute]`. This registers the service interface and its accessor variable name (e.g., `"excel"` → `IExcelService`).
2. **A service interface** — The public API surface (e.g., `IExcelService`, `IMailService`, `ITestingService`).

The `codedWorkflowSupport` flag in `ActivitiesMetadata*.json` is an additional signal — if all activities have `codedWorkflowSupport: false` AND there is no service registry, there is definitively no coded API. However, a domain can have a coded API even when individual activities are marked false (the coded API may expose different operations than the XAML activities).

### Common Patterns

| Domain | Has Coded API | Registry | Service | Pattern |
|--------|:---:|---|---|---|
| Excel | Yes | ExcelRegistry | IExcelService | Handle-based |
| Mail | Yes | MailRegistry | IMailService | Direct methods |
| Testing | Yes | TestingRegistry | ITestingService | Direct methods |
| System | Yes | (in CodedWorkflows) | Multiple services | Direct methods |
| UIAutomationNext | Yes | (in CodedWorkflows) | IUiAutomationAppService | Hybrid |
| OCR, CV, etc. | No | — | — | — |

---

## Workflow: Script-First, Then Document

### Phase 1: Run the Extraction Script

```bash
python {skillPath}/scripts/extract-coded-api.py "{domainRoot}" --pretty --output /tmp/{domain}-coded-api.json
```

The script extracts:
- **Registry**: class name, service accessor variable, service interface type, auto-imported namespaces, assembly attribute
- **Service interface**: all methods with parameters, return types, XML doc comments
- **Handle types**: methods and properties on returned handle/connection types (e.g., `IWorkbookQuickHandle`)
- **Options classes**: properties on configuration POCOs (e.g., `UseOptions`, `WorkbookOptions`)
- **Enums**: enum types defined in the API project
- **Coded workflow support count**: how many activities have `codedWorkflowSupport: true`

### Phase 2: Generate Documentation (You)

Using the script output and the template at [assets/coded-api-template.md](assets/coded-api-template.md), generate `coded/coded-api.md`.

The script gives you the structure; your job is to add:

1. **Service overview** — Describe what the service does and its API pattern (handle-based, direct methods, connection-based).
2. **Method descriptions** — If XML doc comments exist, use them. Otherwise, write concise descriptions based on method names and parameter types.
3. **Return type documentation** — Method return types are the API's **outputs**. Document what each return type represents and how to use it.
4. **Handle operations** — If the API uses handles (IDisposable return types), document all methods/properties on the handle.
5. **Options class properties** — Document all configurable properties with types, defaults, and descriptions.
6. **Common patterns** — Write 3-5 realistic, complete C# examples showing typical usage patterns.

---

## Step-by-Step

### Step 1: Identify Target

Determine which domain to document. The user typically names a package or domain folder.

### Step 2: Run Extraction & Check

```bash
python {skillPath}/scripts/extract-coded-api.py "{domainRoot}" --pretty --output /tmp/{domain}-coded-api.json
```

If `hasCodedApi` is `false`:
- Tell the user this domain has no coded workflow API
- Suggest using the `activity-doc-generator` skill for XAML activity docs instead
- **Stop here**

If `hasCodedApi` is `true` but the script missed things (e.g., the service interface file wasn't found), do targeted manual reads:
```
Grep: pattern="interface I\w+Service" path="{domainRoot}" glob="*.cs"
```

### Step 3: Deep-Read the API Surface

The script extracts method signatures, but you may need to read the actual files for:
- **XML doc comments** that the regex didn't fully capture
- **Handle interface methods** that the script might have partially captured
- **Complex generic types** that need clarification
- **Overloaded methods** — document each overload as a separate entry

Read the files listed in the script output (`serviceInterface.file`, `handleTypes[*].file`, etc.).

### Step 4: Generate the Coded API Doc

Use the template from [assets/coded-api-template.md](assets/coded-api-template.md).

#### Key Sections

**Service Accessor** — The variable name and type that coded workflow developers use:
```csharp
// From the registry's AutoImportedTypes: { "excel", typeof(IExcelService) }
// In coded workflows, use: excel.MethodName(...)
```

**Methods with Outputs** — Every method must document its return type clearly:
```markdown
### `IWorkbookQuickHandle UseExcelFile(string path)`

Opens an Excel file and returns a handle for performing operations on it.

**Parameters:**
- `path` (`string`) — Path to the Excel file

**Returns:** `IWorkbookQuickHandle` — Disposable handle to the workbook. Use with `using` statement.
```

**Handle Types** — These are where the real operations live. Document every public method and property on handle interfaces.

**Auto-Imported Namespaces** — List all namespaces from the registry's `AutoImportedNamespaces`. These are automatically available in coded workflows.

**Common Patterns** — 3-5 real-world examples that:
- Are complete and compilable inside `[Workflow] public void Execute() { ... }`
- Show proper resource cleanup (`using`/`Dispose`)
- Demonstrate realistic scenarios, not just isolated API calls
- Include the required package dependency in `project.json`

### Step 5: Place Output File

Place docs inside the project that **produces the published `.nupkg`** — the one with a `<PackageId>` that is packable. This is NOT always the `*.Package.csproj` — some domains split into design-time and runtime packaging projects. Check `<PackageId>` and `<IsPackable>` to identify the correct project (see [shared/output-structure.md](../../shared/output-structure.md) for details and examples).

```
{PublishedPackageProject}/
  docs/
    coded/
      coded-api.md            # The coded workflow API doc
```

---

## API Patterns Reference

Coded workflow APIs follow one of three patterns:

### 1. Handle-Based (Excel, Word, Presentations)
Service method returns a handle → handle has methods for operations → handle is IDisposable.
```csharp
using var workbook = excel.UseExcelFile("data.xlsx");
var data = workbook.ReadRange("Sheet1");
workbook.WriteRange(data, "Sheet2!A1");
```

### 2. Direct Service Methods (UIAutomation, System, Testing)
Service exposes methods directly. Some return model objects for further chaining.
```csharp
testing.VerifyExpression(actualValue == expectedValue, "Values should match");
```

### 3. Connection-Based (Office365, GSuite)
Service manages cloud connections. Returns connection-specific sub-services.
```csharp
var connection = office365.GetConnection("MyO365Connection");
connection.SendMail(to: "user@example.com", subject: "Hello", body: "...");
```

---

## What NOT to Document

- **Implementation classes** — Only document interfaces (e.g., `IExcelService`, not `ExcelService`)
- **Internal registered types** — The registry's `Register()` method shows DI wiring; don't document it
- **Private/internal methods** — Only public interface members
- **Activity classes** — Those are XAML-side; use `activity-doc-generator` for those

---

## Quality Checklist

- [ ] Service accessor variable name is documented (e.g., `excel`, `mail`, `testing`)
- [ ] Service interface type is documented (e.g., `IExcelService`)
- [ ] All public methods on service interface are listed with full signatures
- [ ] **Method return types (outputs) are clearly documented**
- [ ] Handle types and their methods/properties are documented
- [ ] Handle `IDisposable` status is noted with `using` pattern
- [ ] Options/config classes list all properties with types and defaults
- [ ] Auto-imported namespaces are listed
- [ ] Required package dependency is noted
- [ ] Enum types list their values
- [ ] Examples compile inside a `[Workflow] public void Execute()` method
- [ ] Common patterns show realistic, complete scenarios
