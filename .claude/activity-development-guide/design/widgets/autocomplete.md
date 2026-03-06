# AutoCompleteForExpression Widget

## When to read this

Read this document when you need to:
- Display a searchable dropdown that also supports expressions
- Load options dynamically from a server/API based on user input
- Use `IDynamicDataSourceBuilder` for server-side search

For general DataSource patterns (shared with dropdowns and other widgets), see [../datasources.md](../datasources.md).

---

## Overview

The `AutoCompleteForExpression` widget combines a searchable dropdown with expression support. The user can either select from a list of options or type a VB/C# expression. This is the standard widget for `DesignInArgument<T>` properties that need a predefined list of suggestions.

---

## Static Autocomplete

Use static autocomplete when all options are known at design time and the list is small enough to load in memory.

```csharp
Property.Widget = new DefaultWidget { Type = ViewModelWidgetType.AutoCompleteForExpression };
Property.DataSource = DataSourceBuilder<string>
    .WithId(s => s)
    .WithLabel(s => s)
    .WithInArgumentSingleItemConverter()
    .WithData(items)
    .Build();
```

### Key Methods

| Method | Purpose |
|---|---|
| `WithId(T => string)` | Unique identifier for each item (stored value). |
| `WithLabel(T => string)` | Display text shown in the dropdown. |
| `WithInArgumentSingleItemConverter()` | Converts the selected item to an `InArgument<T>` value. Required for `DesignInArgument<T>` properties. |
| `WithData(IEnumerable<T>)` | Provides the static list of options at build time. |

### Example: Folder name autocomplete

```csharp
FolderName.Widget = new DefaultWidget { Type = ViewModelWidgetType.AutoCompleteForExpression };
FolderName.DataSource = DataSourceBuilder<string>
    .WithId(f => f)
    .WithLabel(f => f)
    .WithInArgumentSingleItemConverter()
    .WithData(new[] { "Inbox", "Sent", "Drafts", "Archive", "Spam" })
    .Build();
```

---

## Dynamic Autocomplete (Server-Side Search)

Use dynamic autocomplete when options are loaded from an API or depend on user input. The `IDynamicDataSourceBuilder` interface enables server-side search -- the framework calls your builder with the user's search text and you return matching results.

```csharp
Property.Widget = new DefaultWidget { Type = ViewModelWidgetType.AutoCompleteForExpression };
Property.SupportsDynamicDataSourceQuery = true;
Property.RegisterService<IDynamicDataSourceBuilder>(new MyDataSourceBuilder());
```

### Required Setup

1. Set `SupportsDynamicDataSourceQuery = true` on the property.
2. Register an `IDynamicDataSourceBuilder` implementation via `RegisterService`.

### IDynamicDataSourceBuilder Implementation

```csharp
internal class MyDataSourceBuilder : IDynamicDataSourceBuilder
{
    public async Task<IDataSource> BuildAsync(
        DynamicDataSourceBuilderContext context,
        CancellationToken cancellationToken)
    {
        var searchText = context.SearchText;

        // Call your API with the search text
        var results = await _apiClient.SearchAsync(searchText, cancellationToken);

        return DataSourceBuilder<MyItem>
            .WithId(item => item.Id)
            .WithLabel(item => item.DisplayName)
            .WithInArgumentSingleItemConverter()
            .WithData(results)
            .Build();
    }
}
```

### Example: Orchestrator queue search

```csharp
QueueName.Widget = new DefaultWidget { Type = ViewModelWidgetType.AutoCompleteForExpression };
QueueName.SupportsDynamicDataSourceQuery = true;
QueueName.RegisterService<IDynamicDataSourceBuilder>(
    new OrchestratorQueueDataSource(_tokenProvider));
```

---

## Combining Static and Dynamic

You can set initial static data and also enable dynamic search. The static data appears immediately, and dynamic results replace them as the user types.

```csharp
Property.Widget = new DefaultWidget { Type = ViewModelWidgetType.AutoCompleteForExpression };
Property.DataSource = DataSourceBuilder<string>
    .WithId(s => s)
    .WithLabel(s => s)
    .WithInArgumentSingleItemConverter()
    .WithData(initialItems)
    .Build();

// Also enable dynamic search for additional results
Property.SupportsDynamicDataSourceQuery = true;
Property.RegisterService<IDynamicDataSourceBuilder>(new MyDynamicSearchBuilder());
```

---

## Troubleshooting

**Autocomplete shows items but selecting one does not set the property value.**
Ensure `WithInArgumentSingleItemConverter()` is called on the `DataSourceBuilder`. Without it, the selected item cannot be converted to an `InArgument<T>` value for the property.

**Dynamic autocomplete never returns results.**
Verify that `SupportsDynamicDataSourceQuery` is set to `true` on the property. Without this flag, the framework does not call the `IDynamicDataSourceBuilder`.

**Search results flicker or show stale data.**
The `IDynamicDataSourceBuilder.BuildAsync` method is called on every keystroke (debounced). Ensure your implementation uses the provided `CancellationToken` to cancel in-flight API requests when new input arrives.

**Selected value clears on workflow reopen.**
The `WithId` selector must return a stable identifier. If the ID is a server-generated GUID that changes between sessions, the framework cannot match the saved value to a dropdown item. In this case, the value is still set on the property (as an expression), but the dropdown may not highlight it.
