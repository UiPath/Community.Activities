# ActivityContext and Workflow Foundation

> **When to read this**: Before writing ANY async activity. This is the single most critical file in this guide. The "Read Before Await" pattern described here prevents the #1 cause of runtime crashes in UiPath activities. If you read nothing else, read this file.

---

## What is ActivityContext?

UiPath activities are built on .NET's Windows Workflow Foundation (WF). `ActivityContext` is a short-lived handle to the workflow execution environment. It provides access to:

- **Arguments**: Read `InArgument<T>` values, write `OutArgument<T>` values
- **Variables**: Read/write workflow variables in scope
- **Extensions**: Access runtime services (`IExecutorRuntime`, `IWorkflowRuntime`)

---

## Context Lifetime Rule (CRITICAL)

> **The context is disposed after the first `await` in async methods.** All argument reads and extension lookups MUST happen before any async operation.

This is explicitly documented in the SDK source:

```csharp
// From SdkActivity.cs:
/// <remarks><paramref name="context"/> will be disposed after the first await</remarks>
protected abstract Task<T> ExecuteAsync(
    AsyncCodeActivityContext context,
    IServiceProvider serviceProvider,
    CancellationToken cancellationToken);
```

**Why**: The workflow runtime reuses context objects. After you yield control (via `await`), the runtime may recycle the context for another activity. Any read/write after that point touches recycled or disposed state.

---

## The "Read Before Await" Pattern

Every async activity MUST follow this 3-step pattern:

```csharp
protected override async Task<Action<AsyncCodeActivityContext>> ExecuteAsync(
    AsyncCodeActivityContext context, CancellationToken cancellationToken)
{
    // STEP 1: Read ALL inputs from context BEFORE any await
    var name = Name.Get(context);
    var timeout = Timeout.Get(context);
    var folderPath = FolderPath.Get(context);
    var labels = Labels.Select(l => l.Get(context)).ToList();  // materialize collections!
    var runtime = context.GetExtension<IWorkflowRuntime>();
    var robotId = runtime.RobotSettings.RobotId;

    // STEP 2: Perform async work (context is now invalid after first await)
    var result = await _service.ProcessAsync(name, timeout, cancellationToken);

    // STEP 3: Return a callback that writes outputs with a NEW context
    return ctx =>
    {
        Result.Set(ctx, result);
        MessageId.Set(ctx, result.Id);
    };
}
```

### What Goes Wrong

```csharp
// WRONG: Reading context after await
protected override async Task<Action<AsyncCodeActivityContext>> ExecuteAsync(
    AsyncCodeActivityContext context, CancellationToken cancellationToken)
{
    var name = Name.Get(context);
    var data = await _service.FetchAsync(name, cancellationToken);

    // Context is disposed here -- this will throw or return stale data
    var format = OutputFormat.Get(context);  // CRASH or WRONG VALUE

    return ctx => { Result.Set(ctx, data); };
}
```

### Checklist

Before submitting any async activity code, verify:

1. All `InArgument.Get(context)` calls are BEFORE the first `await`
2. All `context.GetExtension<T>()` calls are BEFORE the first `await`
3. Collections from context are materialized with `.ToList()` or `.ToArray()` BEFORE the first `await`
4. Output writes (`OutArgument.Set()`) use the callback's `ctx` parameter, NOT the original `context`

---

## Pattern: AsyncCodeActivity (APM Style)

The older `AsyncCodeActivity<T>` uses the Asynchronous Programming Model (APM) with `BeginExecute`/`EndExecute`:

```csharp
public class MyOrchestratorActivity : AsyncCodeActivity<string>
{
    public InArgument<string> AssetName { get; set; }
    public InArgument<int> TimeoutMS { get; set; }

    protected override IAsyncResult BeginExecute(
        AsyncCodeActivityContext context, AsyncCallback callback, object state)
    {
        // Read ALL context values here -- context is invalid after this returns
        var assetName = AssetName.Get(context);
        var timeout = TimeoutMS.Get(context);
        var folderPath = GetFolderPath(context);
        var client = CreateClient(context, timeout, folderPath);

        // Start async work
        return FetchAssetAsync(client, assetName)
            .ToApm(callback, state);
    }

    protected override string EndExecute(
        AsyncCodeActivityContext context, IAsyncResult result)
    {
        // Write outputs here -- context is valid again in EndExecute
        var task = (Task<string>)result;
        return task.Result;
    }
}
```

**Key points**:
- `BeginExecute`: Context is valid. Read all inputs here.
- Between `BeginExecute` and `EndExecute`: Context is invalid. Async work happens here.
- `EndExecute`: Context is valid again. Write outputs here.

---

## Pattern: SdkActivity (Modern SDK)

The SDK wraps the APM pattern into a cleaner async/await model. The SDK creates a **service scope** before the first await, and a **new scope** in the completion callback:

```csharp
public class MyActivity : SdkActivity<string>
{
    public InArgument<string> Input { get; set; }
    public OutArgument<string> Extra { get; set; }

    protected override async Task<string> ExecuteAsync(
        AsyncCodeActivityContext context,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        // Read from context (still valid -- first statement, no await yet)
        var input = Input.Get(context);

        // Services from DI are safe to use across awaits
        var myService = serviceProvider.GetRequiredService<IMyService>();

        // Async work
        return await myService.ProcessAsync(input, cancellationToken);
        // Result is automatically set via the SDK framework
    }
}
```

**Key points**:
- Context reads must still be before the first `await`
- `IServiceProvider` services are safe to use across `await` boundaries (they are captured, not context-bound)
- The return value is automatically assigned to `Result`

See `../advanced/sdk-activity.md` for the full Activities SDK reference.

---

## Pattern: NativeActivity (Scheduling and Bookmarks)

`NativeActivity` uses `NativeActivityContext` which supports scheduling child activities and creating bookmarks. Context values must be captured before scheduling:

```csharp
protected override void Execute(NativeActivityContext context)
{
    // Read ALL values before scheduling child activities
    var timeout = TimeoutMS.Get(context);
    var processName = GetProcessNameValue(context);

    // Store values in implementation variables for access in callbacks
    _timeoutVariable.Set(context, timeout);
    _processNameVariable.Set(context, processName);

    // Schedule child activity -- execution continues in callbacks
    context.ScheduleActivity(_childActivity, OnChildCompleted, OnChildFaulted);
}

// Callback receives a NEW context
private void OnChildCompleted(NativeActivityContext context,
    ActivityInstance completedInstance)
{
    // Read from implementation variables (persisted across async boundaries)
    var processName = _processNameVariable.Get(context);

    // Write outputs
    Result.Set(context, processName);
}
```

**Key points**:
- `Execute`: Read all inputs, store in implementation variables
- Callbacks (`OnChildCompleted`, `OnChildFaulted`): Receive a NEW context. Read state from implementation variables, not from the original context captures.

---

## Parallel Execution and Activity Instances

In parallel execution (`Parallel`, `ParallelForEach`), the **activity instance is shared** but each execution gets its own **context**. This means:

- **Activity properties** (like `InArgument<T>`) are shared across parallel branches
- **Context-bound state** (variables, bookmarks) is per-execution
- **Mutable instance fields** on the activity class are NOT thread-safe

```csharp
// WARNING: From RunJob.cs -- awareness of parallel execution concerns:
//TODO: can these values change in a parallel foreach loop?
// if yes, they should be passed via context
_waitForJobActivity.Timeout = TimeoutMS.Get(context);
```

**Rule**: Never store per-execution state in activity instance fields. Use workflow variables or context-bound implementation variables instead.

---

## Implementation Variables (Cross-Async State)

For `NativeActivity`, use `Variable<T>` to store state across async boundaries. These are per-execution and persisted by the workflow runtime:

```csharp
public class MyNativeActivity : NativeActivity
{
    // Implementation variables -- per-execution, persisted across async
    private readonly Variable<DateTime> _startTime = new(nameof(_startTime));
    private readonly Variable<string> _intermediateResult = new(nameof(_intermediateResult));

    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        base.CacheMetadata(metadata);
        // Register implementation variables so the runtime knows about them
        metadata.AddImplementationVariable(_startTime);
        metadata.AddImplementationVariable(_intermediateResult);
    }

    protected override void Execute(NativeActivityContext context)
    {
        _startTime.Set(context, DateTime.UtcNow);
        context.ScheduleActivity(_childActivity, OnCompleted);
    }

    private void OnCompleted(NativeActivityContext context, ActivityInstance instance)
    {
        var elapsed = DateTime.UtcNow - _startTime.Get(context);
        // Safe: each parallel execution has its own variable value
    }
}
```

**Key points**:
- Declare as `private readonly Variable<T>` fields on the activity class
- Register in `CacheMetadata` via `metadata.AddImplementationVariable()`
- Read/write through context (`_var.Get(context)`, `_var.Set(context, value)`)
- Each parallel execution instance gets its own copy of the variable value

---

## Quick Reference: Context Validity by Pattern

| Pattern | Context valid for reads | Context valid for writes | Cross-await state |
|---------|------------------------|--------------------------|-------------------|
| `CodeActivity` | In `Execute()` | In `Execute()` | N/A (synchronous) |
| `AsyncCodeActivity` | In `BeginExecute()` | In `EndExecute()` | Local variables captured in closure |
| `SdkActivity` | Before first `await` | Return value auto-set | Local variables + `IServiceProvider` |
| `NativeActivity` | In `Execute()` | In callbacks | Implementation variables (`Variable<T>`) |

---

## Troubleshooting

<!-- Agents: add troubleshooting entries here as you encounter common ActivityContext-related issues.
     Format: ### Problem title
             **Symptom**: what the developer sees
             **Cause**: why it happens
             **Fix**: how to resolve it -->
