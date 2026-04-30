# Best Practices

> **When to read this**: Before submitting any activity code for review. This is a checklist of rules distilled from real production issues. Violating the versioning rules in particular can break deployed workflows for all users.

---

## Activity Design

1. **Separate business logic from workflow context.** Put core logic in a public `ExecuteInternal()` method (or similar) that takes plain parameters. This makes unit testing possible without mocking the workflow runtime.

    ```csharp
    // Good: testable business logic separated from context
    public class EmailSender : CodeActivity
    {
        protected override void Execute(CodeActivityContext context)
        {
            var to = To.Get(context);
            var subject = Subject.Get(context);
            var body = Body.Get(context);
            var messageId = SendEmail(to, subject, body);
            MessageId.Set(context, messageId);
        }

        // Public, testable, no workflow dependency
        public string SendEmail(string to, string subject, string body)
        {
            // Business logic here
            return Guid.NewGuid().ToString();
        }
    }
    ```

2. **Use `[RequiredArgument]`** for mandatory properties. The workflow engine validates these before execution.

3. **Use `InArgument<T>`** for inputs that should accept expressions/variables. Use direct types (plain properties) for constants/enums that are always set at design time.

4. **Use `[DefaultValue]`** on all properties for backward compatibility when adding new properties to an existing activity.

5. **Mark sensitive properties** with `[Sensitive]` to exclude them from telemetry and logging.

---

## ViewModel Design

1. **Property names must exactly match** the Activity class property names. A mismatch silently fails -- the property will not be serialized or deserialized.

2. **Always call `base.InitializeModel()`** and `PersistValuesChangedDuringInit()` at the start of `InitializeModel()`:

    ```csharp
    protected override void InitializeModel()
    {
        base.InitializeModel();           // must be first
        PersistValuesChangedDuringInit(); // must be second

        // ... configure properties
    }
    ```

3. **Use `IsPrincipal = true`** for the most important 2-4 properties. These appear in the main panel and cannot be collapsed.

4. **Use `OrderIndex`** to control property display order. Use an incrementing counter:

    ```csharp
    var order = 0;
    To.OrderIndex = order++;
    Subject.OrderIndex = order++;
    Body.OrderIndex = order++;
    ```

5. **Localize all user-visible strings** through resource files. Never hardcode display names:

    ```csharp
    // Good
    To.DisplayName = Resources.EmailSender_To_DisplayName;

    // Bad -- not localizable
    To.DisplayName = "Recipient";
    ```

6. **Use partial classes** for large ViewModels to separate concerns (Configuration, Rules, Actions).

7. **Keep rules focused.** Each rule should handle one concern. Name rules after the property they react to.

See `../design/viewmodel.md` for the full ViewModel reference.

---

## Widget Selection

1. **Default behavior is usually correct.** Only set a widget explicitly when you need something specific.

2. **Use `AutoCompleteForExpression`** when the user should pick from a list but also be able to type an expression.

3. **Use `Dropdown`** when the user must pick from a fixed set of values.

4. **Use `Toggle`** for boolean properties.

5. **Use `PlainNumber`** with Min/Max/Step when you need constrained numeric input without expressions.

6. **Use `TextBlockWidget`** for read-only information display.

7. **Check widget availability** with `HasFeature()` and `IsWidgetSupported()` before using platform-specific widgets:

    ```csharp
    if (workflowDesignApi.HasFeature(DesignFeatureKeys.SomeFeature))
    {
        MyProperty.Widget = new DefaultWidget { Type = ViewModelWidgetType.SpecificWidget };
    }
    ```

See `../design/widgets.md` for the complete widget reference.

---

## Dynamic Data Sources

1. **Use static DataSource** when the list is small and known at design time.

2. **Use `IDynamicDataSourceBuilder`** when data comes from an API or depends on other properties.

3. **Register dependencies** when a data source depends on another property's value.

4. **Add async validators** for data sources that need server-side validation.

See `../design/datasources.md` for DataSource patterns.

---

## Solutions Support

1. **Always check** `IUserDesignContext.SolutionId` to determine if running in a Solution context.

2. **Use `SolutionResourcesWidget`** for Solution-managed resources.

3. **Hide `FolderPath`** when in Solutions context (managed by the Solution):

    ```csharp
    var solutionId = userDesignContext?.SolutionId;
    if (!string.IsNullOrEmpty(solutionId))
    {
        FolderPath.IsVisible = false;
    }
    ```

4. **Provide both paths**: Implement `InitializeProjectScopePropertiesAsync()` and `InitializeSolutionsScopePropertiesAsync()`.

---

## Feature Detection

1. **Always use `HasFeature()`** before using platform-specific APIs:

    ```csharp
    var designApi = Services.GetService<IWorkflowDesignApi>();
    if (designApi?.HasFeature(DesignFeatureKeys.StudioDesignSettingsV3) == true)
    {
        // Safe to use V3 API
    }
    ```

2. **Use `[MethodImpl(MethodImplOptions.NoInlining)]`** when calling methods that reference types from optional assemblies. This prevents JIT from trying to load the assembly when the calling method is compiled:

    ```csharp
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ConfigureDesktopOnlyFeature()
    {
        // References types from an assembly only available on Desktop
    }
    ```

3. **Gracefully degrade** when features are unavailable rather than throwing exceptions.

---

## Performance

1. **Use `ValueTask`** instead of `Task` for async ViewModel methods that often complete synchronously.

2. **Use `Dispatcher.Invoke()`** when updating properties from background threads.

3. **Use `IBusyService`** to show loading indicators during long async operations.

4. **Minimize rule execution.** Set `runOnInit: false` when the rule does not need to run during initialization:

    ```csharp
    Rules.Add(new Rule<MyViewModel>(
        nameof(SomeProperty),
        (ctx) => { /* rule logic */ },
        runOnInit: false  // only runs when SomeProperty changes
    ));
    ```

---

## Testing

1. **Unit test business logic** separately from the workflow context (via the `ExecuteInternal()` pattern).

2. **Workflow test the full activity** using `WorkflowInvoker` with mocked extensions:

    ```csharp
    var invoker = new WorkflowInvoker(new MyActivity());
    invoker.Extensions.Add(mockExecutorRuntime);
    var result = invoker.Invoke(
        new Dictionary<string, object> { ["Input"] = "test" },
        TimeSpan.FromSeconds(10)  // always set a timeout
    );
    ```

3. **ViewModel test** initialization, rules, and property visibility.

4. **Mock `IExecutorRuntime`** to verify logging behavior.

5. **Use `TimeSpan` timeouts** in `WorkflowInvoker.Invoke()` to prevent hanging tests.

---

## Versioning and Compatibility (CRITICAL)

These rules protect deployed workflows from breaking. Violations affect all users who have installed your activity package.

1. **Never rename or delete activity arguments/properties.** Existing `.xaml` workflows serialize activity properties by name. Renaming or removing a property causes a deserialization error when the workflow loads, breaking all projects that use the activity.

2. **New properties require `[DefaultValue]`** so that existing workflows load without errors (the missing property gets the default):

    ```csharp
    // Adding a new property to an existing activity
    [DefaultValue(30000)]
    public InArgument<int> TimeoutMS { get; set; }
    ```

3. **To deprecate a property**: Add `[Obsolete]` and `[Browsable(false)]`. The property must remain on the class -- hide it from the designer, but keep it loadable:

    ```csharp
    [Obsolete("Use TimeoutMS instead")]
    [Browsable(false)]
    public InArgument<int> Timeout { get; set; }
    ```

4. **Minor version bump** for any public property or activity additions.

5. **Major version bump** for breaking changes (e.g., changing a property's type).

---

## Cross-References

- Activity context rules: `activity-context.md` (the "Read Before Await" pattern)
- Project structure and packaging: `project-structure.md`
- Architecture overview: `architecture.md`
- ViewModel patterns: `../design/viewmodel.md`
- Widget reference: `../design/widgets.md`

---

## Troubleshooting

<!-- Agents: add troubleshooting entries here as you encounter common best-practice violations and their symptoms.
     Format: ### Problem title
             **Symptom**: what the developer sees
             **Cause**: why it happens
             **Fix**: how to resolve it -->
