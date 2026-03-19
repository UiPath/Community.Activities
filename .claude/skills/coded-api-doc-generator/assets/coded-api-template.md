# Coded Workflow API Doc Template

This template generates the per-package `coded-api.md` file (placed in the `docs/coded/` directory) documenting the coded workflow API. The coded API is a fundamentally different surface from the XAML activities — it uses service-oriented patterns with method calls, options objects, and opaque handles.

---

## Template

````markdown
# {{PackageDisplayName}} — Coded Workflow API

`{{PackageId}}`

{{PackageDescription}}

**Service accessor:** `{{ServiceVariableName}}` (type `{{ServiceInterfaceType}}`)
**Required package:** `"{{PackageId}}": "[{{Version}}]"` in project.json dependencies

## Auto-Imported Namespaces

These namespaces are automatically available in coded workflows when this package is installed:

```
{{#each autoImportedNamespaces}}
{{this}}
{{/each}}
```

## Service Overview

The `{{ServiceVariableName}}` service provides:

{{ServiceOverview}}

---

{{#each serviceGroups}}
## {{GroupName}}

{{GroupDescription}}

{{#each methods}}
### `{{Signature}}`

{{Description}}

**Parameters:**
{{#each parameters}}
- `{{Name}}` (`{{Type}}`) — {{Description}}{{#if Default}} (default: `{{Default}}`){{/if}}
{{/each}}

**Returns:** `{{ReturnType}}` — {{ReturnDescription}}

{{#if Example}}
```csharp
{{Example}}
```
{{/if}}

{{/each}}
{{/each}}

---

{{#if handleTypes}}
## Handle Types

{{#each handleTypes}}
### `{{HandleName}}`

{{HandleDescription}}

{{#if IsDisposable}}
> This type implements `IDisposable`. Always use inside a `using` statement or call `Dispose()` explicitly.
{{/if}}

#### Methods

| Method | Return Type | Description |
|--------|------------|-------------|
{{#each methods}}
| `{{Signature}}` | `{{ReturnType}}` | {{Description}} |
{{/each}}

#### Properties

| Property | Type | Description |
|----------|------|-------------|
{{#each properties}}
| `{{Name}}` | `{{Type}}` | {{Description}} |
{{/each}}

{{/each}}
{{/if}}

---

{{#if optionsClasses}}
## Options & Configuration Classes

{{#each optionsClasses}}
### `{{ClassName}}`

{{ClassDescription}}

| Property | Type | Default | Description |
|----------|------|---------|-------------|
{{#each properties}}
| `{{Name}}` | `{{Type}}` | {{Default}} | {{Description}} |
{{/each}}

{{/each}}
{{/if}}

---

{{#if enumTypes}}
## Enum Reference

{{#each enumTypes}}
**`{{EnumName}}`**: {{#each values}}`{{this}}`{{#unless @last}}, {{/unless}}{{/each}}
{{/each}}
{{/if}}

---

## Common Patterns

{{#each patterns}}
### {{PatternName}}

```csharp
{{PatternCode}}
```

{{/each}}

````

---

## Field Guidelines

### Service Accessor
The variable name automatically available in coded workflows. Found in the `ICodedWorkflowsServiceRegistry` implementation's `AutoImportedTypes` dictionary:
```csharp
// Example from ExcelRegistry:
AutoImportedTypes = { { "excel", typeof(IExcelService) } }
```
→ Accessor is `excel`, type is `IExcelService`.

### Service Interface
The main public interface. Read it from the `*.API` project. Document only public methods.

### API Patterns
Coded workflow APIs follow one of these patterns:

1. **Handle-based** (Excel, Word, Presentations):
   - Service method returns a handle (`IWorkHandle`, `IWordDocumentHandle`)
   - Handle is `IDisposable` — wrap in `using`
   - Handle has methods for operations (ReadRange, WriteRange, etc.)
   ```csharp
   using var handle = excel.UseExcelFile("file.xlsx");
   var data = handle.ReadRange("Sheet1");
   ```

2. **Direct service methods** (UIAutomation, System, Testing):
   - Service exposes methods directly
   - Some methods return model objects with further chainable methods
   ```csharp
   var app = uiAutomation.Attach(screen);
   app.Click(target);
   ```

3. **Connection-based** (Office365, GSuite):
   - Service manages cloud connections
   - Returns connection-specific sub-services
   ```csharp
   var connection = office365.GetConnection("ConnectionName");
   connection.SendMail(...);
   ```

### Method Signatures and Outputs
Show the full C# method signature including:
- Return type (this IS the method's output — always document what it represents)
- Method name
- All parameters with types
- Default parameter values
- Overloads as separate entries

**Return types are outputs.** Always document:
- What the return type represents (e.g., "a disposable handle to the workbook")
- How to use the returned value (e.g., "use with `using` statement, then call methods on it")
- Whether `null` is possible and what it means
- For `void` methods, note any side effects (what state they change)

### Handle Types
Document the handle/return types thoroughly — this is where the actual operations live. Include:
- All public methods with return types
- All public properties
- Whether it implements `IDisposable`
- The `using` pattern

### Options Classes
POCOs that configure service/method behavior. Document all public properties with types and defaults.

### Common Patterns Section
Show 3-5 real-world patterns that demonstrate typical usage. Each pattern should:
- Be a complete, compilable example inside `[Workflow] public void Execute() { ... }`
- Show a realistic scenario (not just API calls in isolation)
- Include necessary `using` statements as comments
- Demonstrate proper resource cleanup (using/Dispose)

Example patterns to include:
- Basic read/write workflow
- Error handling pattern
- Iterating over collections
- Combining multiple service calls
- Using options objects for configuration
