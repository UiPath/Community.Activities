# DataSource Patterns

> **When to read this**: You are adding a dropdown, autocomplete, or multi-select list to an activity property in the designer. You need to populate options from static data, enums, or a server-side search API.

**Cross-references**: [ViewModel](viewmodel.md) | [Rules and Dependencies](rules-and-dependencies.md)

---

## DataSourceBuilder (Fluent API)

```csharp
Property.DataSource = DataSourceBuilder<T>
    .WithId(x => x.Id)              // Unique identifier (required)
    .WithLabel(x => x.DisplayName)  // Display text (required)
    .WithCategory(x => x.Group)     // Grouping header (optional)
    .WithTooltip(x => x.HelpText)   // Hover tooltip (optional)
    .WithDescription(x => x.Desc)   // Extended description (optional)
    .WithIcon(x => x.IconPath)      // Icon (optional)
    .WithData(itemsList)             // Static data (optional)
    .Build();
```

---

## Converter Patterns

Converters transform between the data source items and the property value.

### InArgument Converter

```csharp
// For InArgument<T> properties — handles argument wrapping automatically
.WithInArgumentSingleItemConverter()
```

### Custom Single-Item Converter

```csharp
.WithSingleItemConverter(
    itemToValue: item => item.ToString(),
    valueToItem: value => items.FirstOrDefault(i => i.ToString() == value))
```

### Enum Converter

```csharp
.WithSingleItemConverter(
    itemToValue: item => item.ToString(),
    valueToItem: value => Enum.TryParse<MyEnum>(value, out var v) ? v : default)
```

---

## Multi-Select DataSource

```csharp
Property.DataSource = DataSourceBuilder<string>
    .WithId(x => x)
    .WithLabel(x => x)
    .WithMultipleSelection(
        selectionToValue: items => new List<string>(items),
        valueToSelection: propertyValue => propertyValue)
    .WithData(allItems)
    .Build();
```

---

## Enum DataSource

```csharp
Property.DataSource = EnumDataSourceBuilder<MyEnum>
    .Configure()
    .WithSingleItemConverter(
        itemToValue: item => item.ToString(),
        valueToItem: value => Enum.TryParse<MyEnum>(value, out var v) ? v : default)
    .WithData(Enum.GetValues<MyEnum>())
    .Build();
```

---

## Dynamic DataSource (Server-Side Search)

Implement `IDynamicDataSourceBuilder` for data that loads on demand:

```csharp
internal class MyDynamicDataSource : IDynamicDataSourceBuilder
{
    public async ValueTask<IDataSource> GetDynamicDataSourceAsync(
        string searchText, int limit, CancellationToken ct = default)
    {
        var results = await _service.SearchAsync(searchText, limit, ct);
        return DataSourceBuilder<string>
            .WithId(x => x)
            .WithLabel(x => x)
            .WithTooltip(x => x)
            .WithData(results)
            .Build();
    }
}
```

Register in the ViewModel:

```csharp
Property.SupportsDynamicDataSourceQuery = true;
Property.RegisterService<IDynamicDataSourceBuilder>(new MyDynamicDataSource(service));
```

---

## Dynamic DataSource with Search (IDynamicDataSourceBuilder)

Full pattern for a searchable autocomplete with on-demand data:

```csharp
// In InitializeModel():
MyProp!.SupportsDynamicDataSourceQuery = true;
MyProp!.DataSource = DataSourceBuilder<string>
    .WithId(s => s.ToLowerInvariant())
    .WithLabel(s => s)
    .WithData(new[] { "Option1", "Option2" })  // initial static data
    .Build();
MyProp!.RegisterService<IDynamicDataSourceBuilder>(new MyDataSourceBuilder());

// Setting SupportsDynamicDataSourceQuery = true implies AutoCompleteForExpression —
// no need to set Widget explicitly.
```

`IDynamicDataSourceBuilder` implementation:

```csharp
internal class MyDataSourceBuilder : IDynamicDataSourceBuilder
{
    private readonly Lazy<IDataSourceConfigured<string>> _config = new(
        () => DataSourceBuilder<string>
            .WithId(x => x.ToLowerInvariant())
            .WithLabel(x => x)
            .Build());

    public IDataSourceConfig GetCurrentConfig() => _config.Value.GetCurrentConfig();

    public bool SupportsFeature(string feature) =>
        feature == IDynamicDataSourceBuilder.FindFeature;

    public async ValueTask<IDataSource> GetDynamicDataSourceAsync(
        string searchText, int limit, int skip, CancellationToken ct = default)
    {
        var data = await FetchDataAsync(searchText, skip, limit);
        return _config.Value.WithData(data).Build();
    }

    public ValueTask<IDataSource> GetDynamicDataSourceAsync(
        string searchText, int limit, CancellationToken ct = default)
        => throw new NotSupportedException();
}
```

---

## Updating DataSource Data After Build

Store the built datasource to set `.Data` later:

```csharp
private DataSource<string> _myDataSource;

protected override void InitializeModel()
{
    base.InitializeModel();
    _myDataSource = DataSourceBuilder<string>
        .WithId(s => s)
        .WithLabel(s => s)
        .WithSingleItemConverter(
            itemToValue: s => new InArgument<string>(s),
            valueToItem: s => GetStringValue(s))
        .Build();
    MyProp!.DataSource = _myDataSource;
    _myDataSource.Data = new string[] { "OptionA", "OptionB" };
}
```

---

## TypeConverter for Complex Types

For a custom type `T` in `DesignProperty<T>`, assign a `TypeConverter` for serialization:

```csharp
// In InitializeModel():
MyProp!.Converter = new MyTypeConverter();

public class MyTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        => MyType.Parse((string)value);

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture,
        object value, Type destinationType)
        => ((MyType)value).ToString();
}
```

---

## Troubleshooting

<!-- AI agents: add entries here when you encounter issues related to this topic -->
