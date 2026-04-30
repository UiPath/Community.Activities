# Orchestrator Integration

> **When to read this**: You are writing or modifying an activity that calls UiPath Orchestrator APIs -- queues, assets, processes, jobs, or any other Orchestrator resource. This covers version detection, API call patterns, folder resolution, error handling, and the base class that provides built-in infrastructure.

**Related files**:
- [Platform API](../runtime/platform-api.md) -- `IOrchestratorSettings`, `IWorkflowRuntime`, and related interfaces
- [Activity Code](../runtime/activity-code.md) -- base class selection and property types

---

## Version Detection Mechanism

Orchestrator version is detected by sending a `HEAD` request to `/odata/$metadata` and parsing the `api-supported-versions` response header. The result is cached globally (one detection per runtime session).

```csharp
// OrchestratorVersion.cs -- called once at runtime, cached globally
public async Task InitVersionAsync(HttpClient client, string orchestratorUrl)
{
    var response = await client.SendAsync(
        new HttpRequestMessage(HttpMethod.Head, orchestratorUrl + "/odata/$metadata"));
    var apiVersions = response.Headers.GetValues("api-supported-versions").First();
    _apiVersions = apiVersions.Split(',').Select(Version.Parse).ToList();
}

public bool SupportsVersion(Version version)
    => _apiVersions is not null && _apiVersions.Count != 0
       && _apiVersions.Min() >= version;
```

Note: `SupportsVersion` checks that the **minimum** supported API version is at least the requested version. This means the Orchestrator supports all versions from `Min` up to `Max` in its declared range.

---

## Predefined Orchestrator Versions

Key version thresholds defined in `OrchestratorVersion.cs`:

```csharp
public static readonly Version ModernUnattendedRobotsOrchestratorVersion = new(10, 0);
public static readonly Version SetRobotAssetVersion = new(13, 0);
public static readonly Version BTSSupport = new(18, 0);
public static readonly Version CreatorUserKey = new(20, 0);
```

Reference these constants instead of hardcoding version numbers to keep version checks readable and centralized.

---

## Version Check Patterns

### Pattern 1: Declare minimum version (framework checks automatically)

The simplest approach. Override `MinSupportedApiVersion` and the base class handles the rest in `EndExecute`.

```csharp
public class BulkAddQueueItems : BaseOrchestratorClientActivity<object>
{
    protected override Version MinSupportedApiVersion => new Version(8, 0);

    // Framework calls ThrowNotSupportedVersionIfNeeded() in EndExecute()
}
```

### Pattern 2: Check before execution

Fail fast at the start of `BeginExecute` when the version requirement is known upfront.

```csharp
protected override IAsyncResult BeginExecute(
    AsyncCodeActivityContext context, AsyncCallback callback, object state)
{
    var version = OrchestratorService.Instance.GetOrchestratorVersion(context);
    if (!version.SupportsVersion(new Version(20, 0)))
    {
        throw OrchestratorExceptionFactory
            .CreateOrchestratorVersionNotSupportedError(new Version(20, 0));
    }
    return base.BeginExecute(context, callback, state);
}
```

### Pattern 3: Branch logic by version

Use different API paths depending on the connected Orchestrator version.

```csharp
var orchestratorVersion = _orchestratorService.GetOrchestratorVersion(context);
if (orchestratorVersion.SupportsVersion(OrchestratorVersion.SetRobotAssetVersion))
{
    await SetAssetValueAsync(client, assetName, value, ct);     // v13.0+ API
}
else
{
    await SetAssetValueLegacyAsync(client, assetName, value, ct); // Pre-13.0 API
}
```

### Pattern 4: Strategy selection by version

For complex multi-version support, use a strategy pattern to encapsulate version-specific logic.

```csharp
// OrchestratorBroker selects different API strategies based on version
public IGetAssetStrategy GetRobotAssetStrategy()
{
    if (!_orchestratorVersion.SupportsVersion(new Version(7, 0)))
        return new GetAssetWithQueryParameters(ctx);       // Pre-7.0

    if (_orchestratorVersion.SupportsVersion(new Version(18, 0)))
        return new GetAssetWithProxySupport(ctx);          // 18.0+

    return new GetAssetWithBodyDefault(ctx);               // 7.0-17.x
}
```

---

## Orchestrator API Call Flow

```
Activity.BeginExecute()
  |-- Read arguments from context (before async)
  |-- Get IWorkflowRuntime -> OrchestratorSettings
  |   |-- QueuesUrl / AssetsUrl / ConfigurationUrl
  |   |-- GetHeaders() -> auth/folder headers
  |   +-- RobotSettings.RobotId
  |-- Create HttpClient with auth middleware stack:
  |   +-- HttpLoggingHandler -> RetryHandler -> BearerTokenHandler
  |-- Set folder path header (Base64 UTF-16LE encoded):
  |   +-- x-uipath-folderpath-encoded: <base64>
  |-- Send request (POST with JSON body)
  +-- Parse response / handle errors
```

Important: Read all arguments from `context` **before** starting async work. The `AsyncCodeActivityContext` is only valid on the workflow thread, not inside `Task.Run` or after an `await`.

---

## Folder Path Resolution

Activities resolve the Orchestrator folder path using a priority-based header system:

| Header | Priority | Description |
|--------|----------|-------------|
| `x-uipath-folderpath-encoded` | Highest | Base64 UTF-16LE encoded folder path |
| `x-uipath-folderpath` | 2nd | Plain text folder path |
| `x-uipath-folderkey` | 3rd | Folder key ID |
| `x-uipath-organizationunitid` | Lowest | Organization unit ID |

The encoded header is preferred because folder names may contain characters that are not safe in HTTP headers.

```csharp
// Encoding the folder path (uses UTF-16LE, not UTF-8)
internal static string ToBase64Header(string value)
    => Convert.ToBase64String(Encoding.Unicode.GetBytes(value ?? string.Empty));
```

**Caution**: `Encoding.Unicode` is UTF-16LE. Do not use `Encoding.UTF8` -- the Orchestrator expects UTF-16LE encoding for this header.

---

## Common Orchestrator API Endpoints

| Resource | Endpoint |
|----------|----------|
| Add queue item | `/odata/Queues/UiPathODataSvc.AddQueueItem` |
| Get transaction item | `/odata/Queues/UiPathODataSvc.GetTransactionItem` |
| Bulk add queue items | `/odata/Queues/UiPathODataSvc.BulkAddQueueItems` |
| Get asset (v7+) | `/odata/Assets/UiPath.Server.Configuration.OData.GetRobotAssetByNameForRobotKey` |
| Get asset (pre-v7) | `/odata/Assets/UiPath.Server.Configuration.OData.GetRobotAsset(robotId='{0}',assetName=@assetName)` |
| Start jobs | `/odata/Jobs/UiPath.Server.Configuration.OData.StartJobs` |

Note: All endpoints are relative to the Orchestrator base URL obtained from `IOrchestratorSettings.ConfigurationUrl` or the specific resource URL (e.g., `QueuesUrl`).

---

## Error Handling Pattern

The `BaseOrchestratorClientActivity.EndExecute()` pattern provides two layers of error handling: version mismatch detection and `ContinueOnError` support.

```csharp
// BaseOrchestratorClientActivity.EndExecute() pattern
protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
{
    try
    {
        var task = (Task)result;
        task.GetAwaiter().GetResult();  // Rethrow if faulted
    }
    catch (Exception ex)
    {
        // Check if error is due to version mismatch
        ThrowNotSupportedVersionIfNeeded(context, ex);

        // ContinueOnError swallows the exception
        if (!ContinueOnError.Get(context)) throw;
    }
}

private void ThrowNotSupportedVersionIfNeeded(
    AsyncCodeActivityContext context, Exception ex)
{
    if (MinSupportedApiVersion == null) return;

    var version = _orchestratorService.GetOrchestratorVersion(context);
    if (version.HasNetworkError()) return;  // Can't determine version

    if (!version.SupportsVersion(MinSupportedApiVersion))
    {
        throw OrchestratorExceptionFactory
            .CreateOrchestratorVersionNotSupportedError(MinSupportedApiVersion, ex);
    }
}
```

Key behaviors:
- `ThrowNotSupportedVersionIfNeeded` wraps the original exception with a more informative "Orchestrator version not supported" message when the root cause is a version mismatch.
- If the version cannot be determined (network error during detection), the original exception propagates unchanged.
- `ContinueOnError` is checked last, allowing version errors to always surface (they indicate a fundamental incompatibility, not a transient failure).

---

## Writing Orchestrator-Integrated Activities Checklist

When writing an activity that calls Orchestrator APIs:

1. **Inherit from `BaseOrchestratorClientActivity<T>`** to get built-in version checking, error handling, and HTTP client management.
2. **Override `MinSupportedApiVersion`** if the activity requires a specific Orchestrator version.
3. **Read context values in `BeginExecute`** before async work begins (the context is thread-bound).
4. **Use `OrchestratorSettings`** for API endpoint URLs and headers (see [Platform API - IOrchestratorSettings](../runtime/platform-api.md#iorchestratorsettings)).
5. **Use `AccessProvider`** for OAuth tokens instead of legacy auth.
6. **Handle version branching** with `SupportsVersion()` when supporting multiple Orchestrator versions.
7. **Register the activity in `ActivitiesBindings.json`** for resource binding support.

---

## Troubleshooting

<!-- Agents: add entries here as you encounter common issues with Orchestrator integration.
     Format: ### Issue Title
             **Symptom**: what the developer sees
             **Cause**: why it happens
             **Fix**: how to resolve it -->
