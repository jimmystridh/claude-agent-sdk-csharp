# Claude Agent SDK for C# - Improvement Plan

## High Priority (Fix Before Release)

### 1. Convert `ClaudeAgentOptions` to a Record

**File:** `src/ClaudeAgentSdk/ClaudeAgentOptions.cs`

**Problem:** Currently a class with init-only setters and a manual `With()` method that must be updated whenever properties are added. Error-prone and verbose.

**Solution:** Convert to a record to get `with` expressions for free:

```csharp
// Before
public sealed class ClaudeAgentOptions
{
    public string? Model { get; init; }
    // ... 25+ properties ...

    public ClaudeAgentOptions With(Action<ClaudeAgentOptions> configure)
    {
        var copy = new ClaudeAgentOptions { /* manually copy all properties */ };
        configure(copy);
        return copy;
    }
}

// After
public sealed record ClaudeAgentOptions
{
    public string? Model { get; init; }
    // ... properties ...
}

// Usage
var newOptions = options with { MaxTurns = 5, Model = "claude-sonnet-4-5" };
```

**Impact:** Also eliminates duplicate copying code in `ClaudeAgent.cs` and `ClaudeAgentClient.cs`.

---

### 2. Fix `SystemPromptPreset` and `ToolsPreset` Hardcoded Values

**File:** `src/ClaudeAgentSdk/Types/McpServer.cs`

**Problem:** The `Preset` property is a getter-only that always returns `"claude_code"`. Users cannot create presets with different values.

```csharp
// Current - broken
public sealed record SystemPromptPreset
{
    public string Preset => "claude_code";  // Can never be anything else!
    public string? Append { get; init; }
}

public sealed record ToolsPreset
{
    public string Preset => "claude_code";  // Same issue
}
```

**Solution:**

```csharp
public sealed record SystemPromptPreset
{
    public string Preset { get; init; } = "claude_code";  // Default but changeable
    public string? Append { get; init; }
}

public sealed record ToolsPreset
{
    public string Preset { get; init; } = "claude_code";
}
```

---

### 3. Thread-Safety for Shared Booleans

**Files:**
- `src/ClaudeAgentSdk/Internal/Transport/SubprocessCliTransport.cs`
- `src/ClaudeAgentSdk/Internal/QueryHandler.cs`

**Problem:** `_isReady` and `_closed` are read/written from multiple threads without synchronization.

```csharp
// SubprocessCliTransport.cs
private bool _isReady;  // Data race!

// QueryHandler.cs
private bool _closed;   // Data race!
```

**Solution:**

```csharp
private volatile bool _isReady;
private volatile bool _closed;
```

---

### 4. Replace Empty Catch Blocks

**Files:** Multiple

**Problem:** Silent exception swallowing loses debugging context:

```csharp
catch { /* ignore */ }
```

**Solution:** At minimum, add diagnostic output:

```csharp
catch (Exception ex)
{
    System.Diagnostics.Debug.WriteLine($"[ClaudeAgentSdk] Ignored: {ex.GetType().Name}: {ex.Message}");
}
```

**Locations to fix:**
- `SubprocessCliTransport.cs:619` - stdin close
- `SubprocessCliTransport.cs:638` - temp file delete
- `SubprocessCliTransport.cs:651` - stderr task wait
- `SubprocessCliTransport.cs:660` - stdin close
- `SubprocessCliTransport.cs:673-674` - process kill
- `QueryHandler.cs:658` - stream input
- `QueryHandler.cs:698` - read task wait
- `ClaudeAgent.cs:118-121` - streaming errors

---

## Medium Priority (Fix Soon)

### 5. Extract Hook Event Name Mapping

**Problem:** Same switch expression duplicated in 3 files.

**Files:**
- `src/ClaudeAgentSdk/ClaudeAgent.cs`
- `src/ClaudeAgentSdk/ClaudeAgentClient.cs`
- `src/ClaudeAgentSdk/Internal/QueryHandler.cs`

**Solution:** Add extension method in `Types/Enums.cs`:

```csharp
public static class HookEventExtensions
{
    public static string ToEventName(this HookEvent evt) => evt switch
    {
        HookEvent.PreToolUse => "PreToolUse",
        HookEvent.PostToolUse => "PostToolUse",
        HookEvent.UserPromptSubmit => "UserPromptSubmit",
        HookEvent.Stop => "Stop",
        HookEvent.SubagentStop => "SubagentStop",
        HookEvent.PreCompact => "PreCompact",
        _ => throw new ArgumentOutOfRangeException(nameof(evt))
    };
}
```

---

### 6. Add Logging Abstraction

**Problem:** Uses `Console.Error.WriteLine` directly.

**File:** `src/ClaudeAgentSdk/Internal/Transport/SubprocessCliTransport.cs:499`

**Solution:** Add optional `ILogger` support:

```csharp
public sealed class SubprocessCliTransport : ITransport
{
    private readonly ILogger? _logger;

    public SubprocessCliTransport(..., ILogger? logger = null)
    {
        _logger = logger;
    }

    // Usage
    _logger?.LogWarning("Claude Code version {Version} is unsupported...", version);
}
```

---

### 7. Handle Background Task Exceptions

**File:** `src/ClaudeAgentSdk/ClaudeAgent.cs:112-122`

**Problem:** Background task exceptions are silently lost.

```csharp
_ = Task.Run(async () =>
{
    try
    {
        await queryHandler.StreamInputAsync(PromptStream(), cancellationToken);
    }
    catch
    {
        // Ignore streaming errors - LOST!
    }
}, cancellationToken);
```

**Solution:** Store exception for later inspection or use a proper error channel:

```csharp
private Exception? _streamingError;

_ = Task.Run(async () =>
{
    try
    {
        await queryHandler.StreamInputAsync(PromptStream(), cancellationToken);
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
        _streamingError = ex;
        System.Diagnostics.Debug.WriteLine($"[ClaudeAgentSdk] Streaming error: {ex}");
    }
}, cancellationToken);
```

---

### 8. Add XML Documentation

**Problem:** Public API has no documentation. `<NoWarn>CS1591</NoWarn>` hides the issue.

**Solution:** Remove NoWarn and add docs to all public types/members:

```csharp
/// <summary>
/// Executes a one-shot query against Claude Code.
/// </summary>
/// <param name="prompt">The user prompt to send.</param>
/// <param name="options">Optional configuration for the query.</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>An async stream of messages from the conversation.</returns>
public static async IAsyncEnumerable<Message> QueryAsync(
    string prompt,
    ClaudeAgentOptions? options = null,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
```

---

### 9. Safe Version Parsing

**File:** `src/ClaudeAgentSdk/Internal/Transport/SubprocessCliTransport.cs:494`

**Problem:** `Version.Parse` throws on malformed input.

```csharp
var version = Version.Parse(match.Groups[1].Value);  // Can throw!
```

**Solution:**

```csharp
if (Version.TryParse(match.Groups[1].Value, out var version))
{
    var minVersion = Version.Parse(MinimumClaudeCodeVersion);
    if (version < minVersion)
    {
        // warn
    }
}
```

---

## Low Priority (Nice to Have)

### 10. Source-Generated JSON Serialization

**Problem:** Runtime reflection-based JSON serialization is slower and not AOT-friendly.

**Solution:** Add a JSON source generator context:

```csharp
[JsonSerializable(typeof(Dictionary<string, object?>))]
[JsonSerializable(typeof(ControlRequest))]
[JsonSerializable(typeof(ControlResponse))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
internal partial class ClaudeJsonContext : JsonSerializerContext { }
```

---

### 11. Builder Pattern for Options

**Problem:** Complex option configuration is verbose.

**Solution:** Add fluent builder:

```csharp
public sealed class ClaudeAgentOptionsBuilder
{
    private readonly ClaudeAgentOptions _options = new();

    public ClaudeAgentOptionsBuilder WithModel(string model)
    {
        _options = _options with { Model = model };
        return this;
    }

    public ClaudeAgentOptionsBuilder WithMaxTurns(int turns) { ... }
    public ClaudeAgentOptionsBuilder AddAllowedTool(string tool) { ... }

    public ClaudeAgentOptions Build() => _options;
}

// Usage
var options = new ClaudeAgentOptionsBuilder()
    .WithModel("claude-sonnet-4-5")
    .WithMaxTurns(5)
    .AddAllowedTool("Read")
    .Build();
```

---

### 12. IConfiguration Support

**Problem:** Configuration is only via code or environment variables.

**Solution:** Add extension for `IConfiguration`:

```csharp
public static class ClaudeAgentOptionsExtensions
{
    public static ClaudeAgentOptions FromConfiguration(IConfiguration config)
    {
        return new ClaudeAgentOptions
        {
            CliPath = config["Claude:CliPath"],
            Model = config["Claude:Model"],
            MaxTurns = config.GetValue<int?>("Claude:MaxTurns"),
            MaxBudgetUsd = config.GetValue<double?>("Claude:MaxBudgetUsd"),
            // ...
        };
    }
}
```

---

### 13. Testability Improvements

**Problem:** `ClaudeAgent` static class is hard to mock.

**Solution:** Add interface:

```csharp
public interface IClaudeAgent
{
    IAsyncEnumerable<Message> QueryAsync(
        string prompt,
        ClaudeAgentOptions? options = null,
        CancellationToken cancellationToken = default);
}

public sealed class ClaudeAgentService : IClaudeAgent
{
    public async IAsyncEnumerable<Message> QueryAsync(...)
    {
        await foreach (var msg in ClaudeAgent.QueryAsync(prompt, options, cancellationToken))
            yield return msg;
    }
}
```

---

## Summary

| Priority | Issue | Effort |
|----------|-------|--------|
| High | Convert ClaudeAgentOptions to record | Medium |
| High | Fix SystemPromptPreset/ToolsPreset | Low |
| High | Add volatile to shared booleans | Low |
| High | Replace empty catch blocks | Low |
| Medium | Extract hook event mapping | Low |
| Medium | Add logging abstraction | Medium |
| Medium | Handle background task exceptions | Low |
| Medium | Add XML documentation | High |
| Medium | Safe version parsing | Low |
| Low | Source-generated JSON | Medium |
| Low | Builder pattern | Medium |
| Low | IConfiguration support | Low |
| Low | Testability (interface) | Low |

## What's Already Good

- Record types for messages (immutable, pattern-matching friendly)
- `IAsyncEnumerable<Message>` for modern async streaming
- `IAsyncDisposable` for proper async cleanup
- `JsonPolymorphic` for STJ polymorphism
- `CancellationToken` throughout
- `Channel<T>` for concurrent message buffering
- `InternalsVisibleTo` for testing
- Clean separation of transport/handler layers
