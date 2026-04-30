# ViewModel Testing

> **When to read this**: You are writing or modifying tests for UiPath activity ViewModels. You need to know how to set up a test for design-time behavior, mock `ModelItem`, use `BaseViewModelUnitTest`, or test property configuration, rules, data sources, and validation. For activity runtime testing, see [activity-testing.md](./activity-testing.md).

---

## Overview

There are four approaches to testing ViewModels, ranging from the fully automated SDK base class to manual mocking for legacy patterns. Choose the approach that matches your ViewModel's base class.

| Approach | Base Class | When to Use |
|---|---|---|
| 1. `BaseViewModelUnitTest` | SDK `DesignPropertiesViewModel` | Recommended for all SDK activities |
| 2. Manual ModelItem mocking | Non-SDK `DesignPropertiesViewModel` | System activities without SDK base classes |
| 3. `ModelTreeManager` + `EditingContext` | Any (designer tests) | Complex model tree manipulation, legacy designer |
| 4. Direct initialization | Any | Testing specific rule behaviors or property interactions |

---

## Approach 1: BaseViewModelUnitTest (SDK Activities) -- Recommended

The SDK provides `BaseViewModelUnitTest` which handles `ModelTreeManager` setup, activity/ViewModel creation, and initialization. This is the recommended approach for SDK-based activities.

```csharp
public class DepositInAccountViewModelTests : BaseViewModelUnitTest
{
    private readonly Mock<IBank> _bankMock = new();
    private readonly IServiceCollection _services = new ServiceCollection();

    public DepositInAccountViewModelTests()
    {
        _services.AddSingleton(_bankMock.Object);
    }

    [Fact]
    public async Task Initialization_SetsPropertyConfiguration()
    {
        var vm = await CreateAndSetupViewModelAsync<DepositInAccount, DepositInAccountViewModel>(
            _services.BuildServiceProvider());

        vm.AccountHolder.IsPrincipal.ShouldBeTrue();
        vm.Deposit.IsRequired.ShouldBeTrue();
        vm.Result.IsPrincipal.ShouldBeFalse();
    }

    [Fact]
    public async Task DataSource_PopulatesFromService()
    {
        var activity = CreateActivity<DepositInAccount>();
        var vm = await CreateAndSetupViewModelAsync(activity,
            new DepositInAccountViewModel(ServicesMock.Object, _services.BuildServiceProvider()));

        vm.AccountHolder.Value = "clientName";
        await AcceptChangesAsync(vm);

        // Trigger dynamic data source
        var ds = await vm.Account.GetService<IDynamicDataSourceBuilder>()
            .GetDynamicDataSourceAsync(string.Empty, int.MaxValue, 0);
        vm.Account.DataSource.Items.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Rule_UpdatesDependentProperties()
    {
        var activity = CreateActivity<DepositInAccount>();
        var vm = await CreateAndSetupViewModelAsync(activity,
            new DepositInAccountViewModel(ServicesMock.Object, _services.BuildServiceProvider()));

        // Select account from data source
        vm.Account.Value = someAccountMetadata;
        await AcceptChangesAsync(vm);

        // Verify rule updated dependent properties
        activity.Currency.Value.ShouldBe(Currency.USD);
        activity.AccountType.Value.ShouldBe(AccountType.Current);
    }

    [Fact]
    public async Task Validation_ShowsErrorWhenRequired()
    {
        var vm = await CreateAndSetupViewModelAsync<DepositInAccount, DepositInAccountViewModel>(
            _services.BuildServiceProvider());

        // Don't set required property
        vm.ValidationErrors.ShouldHaveSingleItem();
        vm.ValidationErrors.First().MemberNames.First()
            .ShouldBe(nameof(DepositInAccountViewModel.Account));
    }
}
```

### What BaseViewModelUnitTest Provides

The base class exposes the following members and methods:

| Member | Description |
|---|---|
| `CreateActivity<T>()` | Creates an activity instance and adds it to the model tree |
| `CreateViewModel<T>()` | Creates a ViewModel with mocked services |
| `CreateAndSetupViewModelAsync()` | Creates the ViewModel, sets `ModelItem`, and calls `InitializeAsync` |
| `AcceptChangesAsync()` | Applies pending changes and triggers rules |
| `CreateViewModelService<T>()` | Registers a mock service in `IDesignServices` |
| `ServicesMock` | Pre-configured `Mock<IDesignServices>` with common services registered |
| `Mtm` | `ModelTreeManager` instance for direct model item manipulation |

Typical test flow:
1. Call `CreateActivity<T>()` or let `CreateAndSetupViewModelAsync` create it automatically.
2. Call `CreateAndSetupViewModelAsync()` to get an initialized ViewModel.
3. Set property values on the ViewModel.
4. Call `AcceptChangesAsync()` to trigger rules.
5. Assert property states, validation errors, or activity values.

---

## Approach 2: Manual ModelItem Mocking (Non-SDK ViewModels)

For ViewModels that inherit directly from `DesignPropertiesViewModel` (not via the SDK), mock `ModelItem` and `IDesignServices` manually.

```csharp
public class EvaluateBusinessRuleViewModelTests
{
    [Fact]
    public async Task ViewModel_CreatesSuccessfully()
    {
        CreateViewModel(out var activityValues, out var model);
        Assert.NotNull(model);
    }

    private void CreateViewModel(out List<DisplayPropertyValue> activityValues,
        out EvaluateBusinessRuleViewModel model)
    {
        // Mock IWorkflowDesignApi and its sub-services
        var workflowDesignApi = new Mock<IWorkflowDesignApi>();
        workflowDesignApi.Setup(s => s.HasFeature(DesignFeatureKeys.WorkflowOperations)).Returns(true);
        workflowDesignApi.Setup(s => s.WorkflowOperationsService)
            .Returns(new Mock<IWorkflowOperationsService>().Object);

        // Mock access provider
        var tokenProvider = new Mock<IAccessProvider>();
        tokenProvider.Setup(s => s.GetResourceUrl(It.IsNotNull<string>()))
            .ReturnsAsync((string scope) => $"localhost://{scope}/resource");

        // Mock dispatcher (required for UI thread operations)
        var dispatcher = new Mock<IViewModelDispatcher>();
        dispatcher.Setup(s => s.Invoke(It.IsAny<Action>())).Callback<Action>(a => a());

        var dispatcherFactory = new Mock<IViewModelDispatcherFactory>();
        dispatcherFactory.Setup(s => s.CreateDispatcher(It.IsAny<IDesignServices>()))
            .Returns(dispatcher.Object);

        // Assemble IDesignServices
        var designServices = new Mock<IDesignServices>();
        designServices.Setup(s => s.GetService<IWorkflowDesignApi>())
            .Returns(workflowDesignApi.Object);
        designServices.Setup(s => s.GetService<IAccessProvider>())
            .Returns(tokenProvider.Object);
        designServices.Setup(s => s.GetService<IViewModelDispatcherFactory>())
            .Returns(dispatcherFactory.Object);

        activityValues = null;
        model = new EvaluateBusinessRuleViewModel(designServices.Object);
    }
}
```

**When to use**: For ViewModels in System activities that do not use the SDK base classes.

Key mocking requirements:
- `IWorkflowDesignApi` -- feature flags and sub-services.
- `IAccessProvider` -- token/URL resolution for activities that call external services.
- `IViewModelDispatcher` / `IViewModelDispatcherFactory` -- required for any ViewModel that dispatches to the UI thread. Mock `Invoke()` to execute the action synchronously.
- `IDesignServices` -- the service locator that ties everything together.

---

## Approach 3: ModelTreeManager with EditingContext (Designer Tests)

For testing ViewModels that manipulate the model tree directly (e.g., collection editors, dynamic argument handling), create a real `ModelTreeManager`:

```csharp
public class ZipFilesViewModelTests
{
    private readonly ZipFilesViewModel _viewModel;
    private readonly CompressFiles _activity;
    private readonly ModelTreeManager _mtm;

    public ZipFilesViewModelTests()
    {
        _activity = new CompressFiles();
        _mtm = new ModelTreeManager(new EditingContext());
        _mtm.Load(_activity);
        _viewModel = new ZipFilesViewModel();
    }

    [Fact]
    public void ShouldSetFilesArgument()
    {
        _viewModel.SetModelItem(_mtm.Root);
        _activity.ContentToArchive.ShouldNotBeNull();
    }

    [Fact]
    public void ShouldAddNewEmptyArgumentIfPreviousOneWasCompleted()
    {
        _viewModel.SetModelItem(_mtm.Root);
        _viewModel.FileModelItems[0].Properties["Argument"]?
            .SetValue(new InArgument<string>("test"));
        _viewModel.FileModelItems.Count.ShouldBe(2);
    }

    [Fact]
    public void PropertyChange_RaisesNotification()
    {
        _viewModel.SetModelItem(_mtm.Root);

        var propertyName = string.Empty;
        _viewModel.PropertyChanged += (_, args) => propertyName = args.PropertyName;
        _viewModel.AttachFolders = true;

        propertyName.ShouldBe(nameof(_viewModel.AttachFolders));
    }
}
```

**When to use**: For ViewModels with complex model tree manipulation or legacy designer patterns.

Key details:
- `new ModelTreeManager(new EditingContext())` creates a real model tree (not mocked).
- `_mtm.Load(_activity)` loads the activity into the tree, making `_mtm.Root` available.
- `_viewModel.SetModelItem(_mtm.Root)` connects the ViewModel to the model tree.
- This approach lets you test `PropertyChanged` notifications, collection manipulation, and model item property changes.

---

## Approach 4: Direct Initialization with Mocked ModelItem

For testing specific ViewModel behaviors (visibility rules, property changes) without full initialization, mock only the `ModelItem` properties you need:

```csharp
[Fact]
public async Task InvokeWorkflowViewModel_ArgumentsVariableInit_ShouldBeVisible()
{
    // Mock specific ModelItem properties
    var mockProp = new Mock<ModelProperty>();
    mockProp.Setup(s => s.Name).Returns(nameof(InvokeWorkflowFile.ArgumentsVariable));
    mockProp.Setup(m => m.ComputedValue).Returns(mockReference);

    var mockModelItem = new Mock<ModelItem>();
    mockModelItem.Setup(s => s.Properties)
        .Returns(new CustomPropCollection(mockProp.Object));

    // Create and initialize ViewModel
    CreateViewModel(out var activityValues, out var model);
    model.ModelItem = mockModelItem.Object;
    await model.InitializeAsync(activityValues);

    // Assert rule results
    Assert.True(model.ArgumentsVariable.IsVisible);
    Assert.False(model.Arguments.IsVisible);
}
```

**When to use**: When testing specific rule behaviors or property interactions where full model tree setup is unnecessary.

Key details:
- Mock only the `ModelProperty` instances you need via `Mock<ModelProperty>`.
- Use `CustomPropCollection` (or equivalent) to return those properties from `ModelItem.Properties`.
- Set `ModelItem` directly on the ViewModel, then call `InitializeAsync`.
- This approach is fast but less realistic than Approaches 1-3.

---

## What to Test for ViewModels

| Aspect | How to Test |
|---|---|
| Property configuration | Assert `IsPrincipal`, `IsRequired`, `IsVisible`, `OrderIndex` after init |
| Widget assignment | Assert `property.Widget.Type` matches expected widget |
| Rules execution | Set triggering property, call `AcceptChangesAsync`, assert results |
| Visibility toggling | Set mode property, verify dependent properties' `IsVisible` |
| DataSource population | Trigger data source, assert `DataSource.Items.Count` |
| Validation | Leave required properties empty, assert `ValidationErrors` |
| Menu actions | Verify menu actions registered with correct display names |
| Property sync to activity | Change VM property, call `AcceptChangesAsync`, read activity property |

---

## Cross-References

- [Activity Testing](./activity-testing.md) -- runtime testing with WorkflowInvoker, service mocks, and test helpers.
- [ViewModel Patterns](../design/viewmodel-patterns.md) -- ViewModel implementation patterns that inform what to test.
- [Activity Anatomy](../core/activity-anatomy.md) -- how activities are structured, relevant for understanding what the ViewModel wraps.

---

## Troubleshooting

<!-- AI agents: add entries here when you encounter issues related to this topic -->
