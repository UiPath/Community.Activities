# Activity Testing

> **When to read this**: You are writing or modifying tests for UiPath activities (not ViewModels). You need to know how to set up a test project, configure arguments, use WorkflowInvoker, mock runtime services, or follow testing best practices. For ViewModel-specific testing, see [viewmodel-testing.md](./viewmodel-testing.md).

---

## Test Project Setup

Every activity test project requires these NuGet references in the `.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
  <PackageReference Include="Moq" Version="4.20.72" />
  <PackageReference Include="xunit" Version="2.9.3" />
  <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
  <PackageReference Include="Shouldly" Version="4.2.1" />
  <PackageReference Include="UiPath.Activities.Api" Version="24.10.1"
                    DevelopmentDependency="true" />
  <PackageReference Include="UiPath.Workflow" Version="6.0.0-*" PrivateAssets="All" />
</ItemGroup>
```

- **xunit** + **xunit.runner.visualstudio**: Test framework and runner.
- **Moq**: Mocking framework for interfaces (`IExecutorRuntime`, `IWorkflowRuntime`, etc.).
- **Shouldly**: Fluent assertions (`value.ShouldBe(expected)`), used alongside xUnit `Assert`.
- **UiPath.Activities.Api**: Provides `IExecutorRuntime`, `IWorkflowRuntime`, and other runtime contracts.
- **UiPath.Workflow**: Provides `WorkflowInvoker`, `CodeActivity`, `Sequence`, and WF4 types.

---

## Setting InArgument/OutArgument Values in Tests

Activities expose `InArgument<T>` and `OutArgument<T>` properties. In tests, set and read them as follows:

```csharp
// Setting InArgument values (multiple syntax options)
activity.MyProp = new InArgument<string>("literal value");
activity.MyProp = new InArgument<string>(ctx => "expression");
activity.MyProp = InArgument<string>.FromValue("literal");
activity.MyProp = 42;  // implicit conversion for simple types

// Reading OutArgument values after execution
var result = WorkflowInvoker.Invoke(activity);
var output = result["OutputPropertyName"];  // key = property name
```

The result dictionary returned by `WorkflowInvoker.Invoke()` is keyed by the `OutArgument` property name on the activity class.

---

## WorkflowInvoker: Variable&lt;T&gt; Limitation

`Variable<T>` requires an enclosing activity scope (`Variables` collection). `WorkflowInvoker` does not provide one, so using a `Variable<T>`-backed `OutArgument` causes `InvalidWorkflowException`.

```csharp
// WRONG — Variable<T> without enclosing scope
var output = new Variable<string>("result");
var activity = new MyActivity { Output = new OutArgument<string>(output) }; // throws!

// CORRECT — bare OutArgument, read from Invoke() dictionary
var activity = new MyActivity
{
    Input = new InArgument<string>("test"),
    Output = new OutArgument<string>()
};

var runner = new WorkflowInvoker(activity);
runner.Extensions.Add(() => _runtimeMock.Object);
var result = runner.Invoke(TimeSpan.FromSeconds(5));

// Key = the property name on the activity class
Assert.NotNull(result["Output"]);
```

---

## Three Levels of Activity Testing

There are three levels of activity testing, from simplest to most comprehensive. Use all three where applicable.

### Level 1: Unit Tests (Business Logic Only)

Extract business logic into a public method (e.g., `ExecuteInternal`) and test it directly, without any workflow infrastructure.

```csharp
public class CalculatorUnitTests
{
    [Theory]
    [InlineData(1, Operation.Add, 1, 2)]
    [InlineData(3, Operation.Subtract, 2, 1)]
    [InlineData(3, Operation.Multiply, 2, 6)]
    [InlineData(6, Operation.Divide, 2, 3)]
    public void Calculator_ReturnsExpected(int a, Operation op, int b, int expected)
    {
        var calculator = new Calculator { SelectedOperation = op };
        var result = calculator.ExecuteInternal(a, b);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Calculator_Divide_ThrowsOnZero()
    {
        var calculator = new Calculator { SelectedOperation = Operation.Divide };
        Assert.Throws<DivideByZeroException>(() => calculator.ExecuteInternal(4, 0));
    }
}
```

**When to use**: Always. Every activity should have unit tests for its core logic.

### Level 2: WorkflowInvoker Tests (Activity Execution)

Test the full activity execution in a workflow context using `WorkflowInvoker`. This validates argument binding, execution flow, and output mapping.

```csharp
public class CalculatorWorkflowTests
{
    private readonly Mock<IExecutorRuntime> _runtimeMock = new();

    [Fact]
    public void Divide_ReturnsExpected()
    {
        var activity = new Calculator
        {
            FirstNumber = 4,
            SecondNumber = 2,
            SelectedOperation = Operation.Divide
        };

        var runner = new WorkflowInvoker(activity);
        runner.Extensions.Add(() => _runtimeMock.Object);

        var result = runner.Invoke(TimeSpan.FromSeconds(1));

        Assert.Equal(2, result["Result"]);
        _runtimeMock.Verify(x => x.LogMessage(It.IsAny<LogMessage>()), Times.Once);
    }

    [Fact]
    public void Divide_ThrowsOnZero()
    {
        var activity = new Calculator
        {
            FirstNumber = 4,
            SecondNumber = 0,
            SelectedOperation = Operation.Divide
        };

        var runner = new WorkflowInvoker(activity);
        runner.Extensions.Add(() => _runtimeMock.Object);

        Assert.Throws<DivideByZeroException>(() => runner.Invoke(TimeSpan.FromSeconds(1)));
    }
}
```

**When to use**: For all activities. Verifies the activity works correctly in a workflow context.

Key points:
- Always pass a `TimeSpan` timeout to `runner.Invoke()` to prevent hanging tests.
- Register extensions via factory lambda (`() => mock.Object`) for lazy resolution.
- Verify `IExecutorRuntime.LogMessage` calls to confirm the activity logs correctly.

### Level 3: WorkflowInvoker with Service Mocks (Integration)

For activities that interact with external services (Orchestrator, APIs, HTTP clients), mock the service interfaces and register them as workflow extensions.

```csharp
public class GetAssetTests
{
    [Theory]
    [InlineData(true, "test name", "Force")]
    [InlineData(false, "Kane", true)]
    public void GetAssetRequest(bool supportVersion, string assetName, object assetValue)
    {
        // Mock Orchestrator services
        var httpService = new Mock<IHttpClientService>();
        httpService.Setup(a => a.Create(It.IsAny<ActivityContext>(), It.IsAny<int>(), It.IsAny<string>()))
            .Returns(new HttpClient());

        var robotSettings = new Mock<IRobotSettings>();
        robotSettings.Setup(a => a.RobotId).Returns(Guid.Empty);

        var orchestratorSettings = new Mock<IOrchestratorSettings>();
        orchestratorSettings.Setup(a => a.QueuesUrl).Returns("http://someUrl");

        // Mock workflow runtime (provides settings to activity)
        var workflowRuntime = new Mock<IWorkflowRuntime>();
        workflowRuntime.SetupGet(w => w.RobotSettings).Returns(robotSettings.Object);
        workflowRuntime.SetupGet(w => w.OrchestratorSettings).Returns(orchestratorSettings.Object);

        // Create and run activity
        var activity = new GetRobotAsset { AssetName = assetName };
        var runner = new WorkflowInvoker(activity);
        runner.Extensions.Add(workflowRuntime.Object);

        var result = runner.Invoke(TimeSpan.FromSeconds(10));

        // Verify interactions
        httpService.Verify(a => a.Create(
            It.IsAny<ActivityContext>(), It.IsAny<int>(), expectedFolderPath), Times.Once);
    }
}
```

**When to use**: For activities that depend on Orchestrator, HTTP services, or other external systems. Mock every external dependency and verify the interactions.

---

## Testing with IWorkflowExecutor (Invoke Activities)

Activities that invoke other workflows (`InvokeWorkflowFile`, `InvokeProcess`) use `IWorkflowExecutor`. Mock its async begin/end pattern:

```csharp
[Theory]
[MemberData(nameof(GetInvokeActivities))]
public void ExecutesAndReturnsResults(ExecutorInvokeActivity activity)
{
    activity.Arguments = new Dictionary<string, Argument>
    {
        { "OutArg1", new OutArgument<string>() },
        { "InArg1", new InArgument<string>("inputValue") }
    };

    Mock<IWorkflowExecutor> executorMock = null;

    var runtimeMock = new Mock<IWorkflowRuntime>();
    runtimeMock.Setup(a => a.HasFeature(It.IsAny<string>())).Returns(true);
    runtimeMock.Setup(a => a.CreateWorkflowExecutor(It.IsAny<WorkflowParameters>()))
        .Returns<WorkflowParameters>(p =>
        {
            executorMock = new Mock<IWorkflowExecutor>();
            executorMock.Setup(e => e.BeginExecute(It.IsAny<AsyncCallback>(), It.IsAny<object>()))
                .Returns<AsyncCallback, object>((cb, st) =>
                {
                    var tcs = new TaskCompletionSource<bool>(st);
                    tcs.TrySetResult(true);
                    Task.Run(() => cb(tcs.Task));
                    return tcs.Task;
                });
            executorMock.Setup(e => e.EndExecute(It.IsAny<IAsyncResult>()))
                .Returns(new Dictionary<string, object> { { "OutArg1", "result" } });
            return executorMock.Object;
        });

    var runner = new WorkflowInvoker(activity);
    runner.Extensions.Add(() => runtimeMock.Object);
    var result = runner.Invoke(TimeSpan.FromSeconds(5));

    executorMock.Verify(e => e.EndExecute(It.IsAny<IAsyncResult>()), Times.Once);
}
```

Key details:
- `IWorkflowExecutor` uses the APM (Asynchronous Programming Model) pattern with `BeginExecute`/`EndExecute`.
- The `TaskCompletionSource` simulates async completion synchronously in the test.
- `Task.Run(() => cb(tcs.Task))` ensures the callback fires on a separate thread, matching real runtime behavior.

---

## Test Helper Extension Methods

Create reusable helpers to reduce boilerplate across test classes:

```csharp
public static class ActivityExtensions
{
    public static IDictionary<string, object> TestActivity(
        this Activity activity, Action<WorkflowInvoker> setup = null)
    {
        var runner = new WorkflowInvoker(activity);
        setup?.Invoke(runner);
        return runner.Invoke();
    }

    public static IDictionary<string, object> TestActivity(
        this Activity activity, IExecutorRuntime runtime, IWorkflowRuntime wfRuntime)
    {
        return activity.TestActivity(runner =>
        {
            runner.Extensions.Add(runtime);
            runner.Extensions.Add(wfRuntime);
        });
    }

    public static Sequence AddActivity(this Sequence sequence, Activity activity)
    {
        sequence.Activities.Add(activity);
        return sequence;
    }
}
```

These helpers let you write concise tests:

```csharp
var result = myActivity.TestActivity(runner =>
{
    runner.Extensions.Add(() => runtimeMock.Object);
});
```

---

## Testing Multiple Activities in a Sequence

To test that multiple activities execute in order and interact correctly, compose them into a `Sequence`:

```csharp
[Fact]
public void MultipleActivities_ExecuteInOrder()
{
    var activity1 = new MyActivity { Input = "first" };
    var activity2 = new MyActivity { Input = "second" };

    var workflow = new Sequence()
        .AddActivity(activity1)
        .AddActivity(activity2);

    var ex = Record.Exception(() => workflow.TestActivity());
    ex.ShouldBeNull();

    _serviceMock.Verify(x => x.Process("first"), Times.Once);
    _serviceMock.Verify(x => x.Process("second"), Times.Once);
}
```

This uses the `AddActivity` extension method and `TestActivity` helper defined above.

---

## Tracking Activity Execution with Test Helpers

For tests that need to observe execution count or captured values, create a tracking test activity:

```csharp
// Helper activity that tracks execution
public class TestActivity<T> : CodeActivity
{
    public int ExecutionCount { get; set; }
    public List<T> ReceivedValues { get; } = new();
    public InArgument<T> ValueIn { get; set; }

    protected override void Execute(CodeActivityContext context)
    {
        ExecutionCount++;
        ReceivedValues.Add(context.GetValue(ValueIn));
    }
}
```

Use this in tests to verify how many times an activity was executed and what values it received:

```csharp
var tracker = new TestActivity<string>();
// ... add to workflow, execute ...
tracker.ExecutionCount.ShouldBe(3);
tracker.ReceivedValues.ShouldContain("expected");
```

---

## Testing Best Practices

1. **Always test business logic separately** via `ExecuteInternal()` methods -- no workflow infrastructure needed.
2. **Use `WorkflowInvoker` with timeouts** -- prevent hanging tests: `runner.Invoke(TimeSpan.FromSeconds(5))`.
3. **Register extensions via factory** -- `runner.Extensions.Add(() => mock.Object)` for lazy resolution.
4. **Mock `IExecutorRuntime`** to verify logging: `_runtimeMock.Verify(x => x.LogMessage(...), Times.Once)`.
5. **Mock `IViewModelDispatcher`** in ViewModel tests to handle `Dispatcher.Invoke()` calls synchronously.
6. **Use `AcceptChangesAsync()`** after property changes to trigger rules and validate results.
7. **Prefer `[Theory]` with `[InlineData]`** for parameterized tests, `[MemberData]` for complex data.
8. **Test error paths** -- divide by zero, null inputs, missing required properties, timeout scenarios.
9. **For SDK activities**, inherit from `BaseViewModelUnitTest` -- it handles all the boilerplate.
10. **Use `Shouldly`** assertions (`value.ShouldBe(expected)`) for readable test output alongside xUnit `Assert`.

---

## Cross-References

- [ViewModel Testing](./viewmodel-testing.md) -- testing approaches for activity ViewModels and design-time behavior.
- [Activity Anatomy](../core/activity-anatomy.md) -- how activities are structured (CodeActivity, AsyncCodeActivity, NativeActivity).
- [ViewModel Patterns](../design/viewmodel-patterns.md) -- ViewModel implementation patterns that inform what to test.

---

## Troubleshooting

<!-- AI agents: add entries here when you encounter issues related to this topic -->

### InvalidWorkflowException with Variable<T> in WorkflowInvoker

**Symptom**: `InvalidWorkflowException: The following errors were encountered while processing the workflow tree` when using `new OutArgument<T>(variable)` in a WorkflowInvoker test.
**Cause**: `Variable<T>` must be declared in an enclosing activity's `Variables` collection. `WorkflowInvoker` doesn't provide one.
**Fix**: Use bare `new OutArgument<T>()` and read the result from the dictionary returned by `Invoke()` using the property name as key.
