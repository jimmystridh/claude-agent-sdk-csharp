# Claude Agent SDK - C# Port Plan

## Overview

Port the Python `claude-agent-sdk` to an idiomatic C# library, maintaining feature parity while leveraging C# language features and .NET ecosystem conventions.

## Architecture Mapping

### Python → C# Equivalents

| Python | C# |
|--------|-----|
| `async def` / `await` | `async Task` / `await` |
| `AsyncIterator[T]` | `IAsyncEnumerable<T>` |
| `async with` | `await using` / `IAsyncDisposable` |
| `dataclass` | `record` or `class` with init properties |
| `Union[A, B, C]` | Abstract base class + sealed derived classes |
| `Literal["a", "b"]` | `enum` or string constants |
| `dict[str, Any]` | `Dictionary<string, object?>` or `JsonElement` |
| `Callable` | `Func<T>` or `delegate` |
| `anyio.create_task_group()` | `Task.WhenAll` or custom task management |
| `anyio.create_memory_object_stream()` | `System.Threading.Channels.Channel<T>` |
| `anyio.Event()` | `TaskCompletionSource<T>` |
| `anyio.Lock()` | `SemaphoreSlim(1, 1)` |
| Pattern matching (`match/case`) | Switch expressions |

---

## Project Structure

```
claude-agent-sdk-csharp/
├── src/
│   └── ClaudeAgentSdk/
│       ├── ClaudeAgentSdk.csproj
│       ├── ClaudeAgentClient.cs          # Main streaming client
│       ├── ClaudeAgentQuery.cs           # Static query methods
│       ├── ClaudeAgentOptions.cs         # Configuration options
│       ├── Types/
│       │   ├── Messages.cs               # Message types (User, Assistant, System, Result, StreamEvent)
│       │   ├── ContentBlocks.cs          # Text, Thinking, ToolUse, ToolResult blocks
│       │   ├── ControlProtocol.cs        # SDK control request/response types
│       │   ├── Hooks.cs                  # Hook input/output types
│       │   ├── Permissions.cs            # Tool permission types
│       │   ├── McpServer.cs              # MCP server configuration types
│       │   └── Enums.cs                  # PermissionMode, HookEvent, etc.
│       ├── Mcp/
│       │   ├── SdkMcpServer.cs           # In-process MCP server
│       │   ├── ToolAttribute.cs          # [Tool] attribute for declarative tools
│       │   └── McpToolRegistry.cs        # Tool registration and invocation
│       ├── Internal/
│       │   ├── InternalClient.cs         # Internal orchestration
│       │   ├── QueryHandler.cs           # Control protocol handler
│       │   ├── MessageParser.cs          # JSON → typed message conversion
│       │   └── Transport/
│       │       ├── ITransport.cs         # Transport interface
│       │       └── SubprocessCliTransport.cs  # CLI subprocess transport
│       ├── Exceptions/
│       │   ├── ClaudeAgentException.cs   # Base exception
│       │   ├── CliConnectionException.cs
│       │   ├── CliNotFoundException.cs
│       │   ├── ProcessException.cs
│       │   ├── JsonDecodeException.cs
│       │   └── MessageParseException.cs
│       └── Json/
│           ├── JsonSerializerOptionsProvider.cs
│           └── Converters/               # Custom JSON converters
├── tests/
│   └── ClaudeAgentSdk.Tests/
│       ├── ClaudeAgentSdk.Tests.csproj
│       ├── QueryTests.cs
│       ├── ClientTests.cs
│       ├── MessageParserTests.cs
│       ├── TransportTests.cs
│       ├── ToolPermissionTests.cs
│       ├── HookTests.cs
│       └── McpIntegrationTests.cs
├── examples/
│   └── ClaudeAgentSdk.Examples/
│       ├── ClaudeAgentSdk.Examples.csproj
│       ├── QuickStart/
│       ├── StreamingMode/
│       ├── McpCalculator/
│       ├── Hooks/
│       ├── ToolPermissions/
│       └── ... (all examples from Python)
├── ClaudeAgentSdk.sln
├── Directory.Build.props             # Shared project settings
├── Directory.Packages.props          # Central package management
└── README.md
```

---

## Implementation Plan

### Phase 1: Foundation (Types & Infrastructure)

#### 1.1 Project Setup
- [ ] Create solution and project structure
- [ ] Configure .NET 8.0+ target framework
- [ ] Set up nullable reference types, implicit usings
- [ ] Configure `Directory.Build.props` for shared settings
- [ ] Add NuGet package references (System.Text.Json, etc.)

#### 1.2 Exception Hierarchy
- [ ] `ClaudeAgentException` - base class
- [ ] `CliConnectionException` - connection failures
- [ ] `CliNotFoundException` - CLI binary not found
- [ ] `ProcessException` - process exit with error
- [ ] `JsonDecodeException` - JSON parsing failures
- [ ] `MessageParseException` - unknown message format

#### 1.3 Enums
- [ ] `PermissionMode` - Default, AcceptEdits, Plan, BypassPermissions
- [ ] `HookEvent` - PreToolUse, PostToolUse, UserPromptSubmit, Stop, SubagentStop, PreCompact
- [ ] `MessageType` - User, Assistant, System, Result, StreamEvent

#### 1.4 Content Block Types
```csharp
public abstract record ContentBlock;

public sealed record TextBlock(string Text) : ContentBlock;

public sealed record ThinkingBlock(string Thinking, string Signature) : ContentBlock;

public sealed record ToolUseBlock(
    string Id,
    string Name,
    JsonElement Input
) : ContentBlock;

public sealed record ToolResultBlock(
    string ToolUseId,
    JsonElement? Content,
    bool? IsError
) : ContentBlock;
```

#### 1.5 Message Types
```csharp
public abstract record Message;

public sealed record UserMessage(
    OneOf<string, IReadOnlyList<ContentBlock>> Content,
    string? Uuid = null,
    string? ParentToolUseId = null
) : Message;

public sealed record AssistantMessage(
    IReadOnlyList<ContentBlock> Content,
    string Model,
    string? ParentToolUseId = null,
    AssistantMessageError? Error = null
) : Message;

public sealed record SystemMessage(
    string Subtype,
    JsonElement Data
) : Message;

public sealed record ResultMessage(
    string Subtype,
    int DurationMs,
    int DurationApiMs,
    bool IsError,
    int NumTurns,
    string SessionId,
    double? TotalCostUsd,
    JsonElement? Usage,
    JsonElement? StructuredOutput
) : Message;

public sealed record StreamEvent(
    string Uuid,
    string SessionId,
    JsonElement Event,
    string? ParentToolUseId = null
) : Message;
```

#### 1.6 Control Protocol Types
- [ ] `SdkControlRequest` and subtypes
- [ ] `SdkControlResponse` types
- [ ] `ControlInitializeRequest` / `Response`
- [ ] `ControlPermissionRequest`
- [ ] `HookCallbackRequest`
- [ ] `McpMessageRequest`
- [ ] `InterruptRequest`
- [ ] `RewindFilesRequest`
- [ ] `SetPermissionModeRequest`
- [ ] `SetModelRequest`

#### 1.7 Hook Types
```csharp
public abstract record HookInput
{
    public required string SessionId { get; init; }
    public required string TranscriptPath { get; init; }
    public required string Cwd { get; init; }
    public string? PermissionMode { get; init; }
}

public sealed record PreToolUseHookInput : HookInput
{
    public string HookEventName => "PreToolUse";
    public required string ToolName { get; init; }
    public required JsonElement ToolInput { get; init; }
}

// PostToolUseHookInput, UserPromptSubmitHookInput, etc.

public record HookOutput
{
    public bool? Continue { get; init; }
    public bool? SuppressOutput { get; init; }
    public string? StopReason { get; init; }
    public string? Decision { get; init; }
    public string? SystemMessage { get; init; }
    public string? Reason { get; init; }
    public JsonElement? HookSpecificOutput { get; init; }
}

public record AsyncHookOutput
{
    public bool Async { get; init; } = true;
    public int? AsyncTimeout { get; init; }
}
```

#### 1.8 Permission Types
```csharp
public abstract record PermissionResult;

public sealed record PermissionAllow(
    JsonElement? UpdatedInput = null,
    IReadOnlyList<PermissionUpdate>? UpdatedPermissions = null
) : PermissionResult
{
    public string Behavior => "allow";
}

public sealed record PermissionDeny(
    string Message,
    bool Interrupt = false
) : PermissionResult
{
    public string Behavior => "deny";
}
```

#### 1.9 MCP Server Types
```csharp
public abstract record McpServerConfig;

public sealed record McpStdioServerConfig(
    string Command,
    IReadOnlyList<string>? Args = null,
    IReadOnlyDictionary<string, string>? Env = null
) : McpServerConfig
{
    public string Type => "stdio";
}

public sealed record SdkMcpServerConfig(
    ISdkMcpServer Instance
) : McpServerConfig
{
    public string Type => "sdk";
}
```

#### 1.10 Options Class
```csharp
public sealed class ClaudeAgentOptions
{
    // Tool configuration
    public object? Tools { get; init; }  // list<string> | ToolPreset | null
    public IReadOnlyList<string>? AllowedTools { get; init; }
    public IReadOnlyList<string>? DisallowedTools { get; init; }
    public PermissionMode? PermissionMode { get; init; }

    // Model configuration
    public string? Model { get; init; }
    public string? FallbackModel { get; init; }

    // Session management
    public bool? ContinueConversation { get; init; }
    public string? Resume { get; init; }
    public bool? ForkSession { get; init; }

    // Limits
    public int? MaxTurns { get; init; }
    public double? MaxBudgetUsd { get; init; }

    // System prompt
    public object? SystemPrompt { get; init; }  // string | SystemPromptPreset

    // MCP servers
    public object? McpServers { get; init; }  // dict | path

    // Callbacks
    public Func<string, JsonElement, ToolPermissionContext, Task<PermissionResult>>? CanUseTool { get; init; }
    public IReadOnlyDictionary<HookEvent, IReadOnlyList<HookMatcher>>? Hooks { get; init; }
    public Action<string>? Stderr { get; init; }

    // Execution
    public string? Cwd { get; init; }
    public string? CliPath { get; init; }
    public IReadOnlyDictionary<string, string>? Env { get; init; }
    public IReadOnlyList<string>? AddDirs { get; init; }

    // Advanced
    public int? MaxThinkingTokens { get; init; }
    public IReadOnlyList<string>? Betas { get; init; }
    public bool? EnableFileCheckpointing { get; init; }
    public SandboxSettings? Sandbox { get; init; }
    public JsonElement? OutputFormat { get; init; }
    public bool? IncludePartialMessages { get; init; }
    public int? MaxBufferSize { get; init; }
}
```

---

### Phase 2: Transport Layer

#### 2.1 Transport Interface
```csharp
public interface ITransport : IAsyncDisposable
{
    Task ConnectAsync(CancellationToken cancellationToken = default);
    IAsyncEnumerable<JsonElement> ReadMessagesAsync(CancellationToken cancellationToken = default);
    Task WriteAsync(JsonElement data, CancellationToken cancellationToken = default);
    Task CloseAsync(CancellationToken cancellationToken = default);
}
```

#### 2.2 Subprocess CLI Transport
- [ ] CLI discovery logic (bundled, PATH, common locations)
- [ ] Command-line argument building from options
- [ ] Process management with `System.Diagnostics.Process`
- [ ] Async stdin/stdout/stderr handling
- [ ] JSON line reading and parsing
- [ ] Graceful shutdown and cleanup

#### 2.3 Message Parser
- [ ] Parse JSON to typed `Message` objects
- [ ] Content block type dispatch (text, thinking, tool_use, tool_result)
- [ ] Handle control protocol messages
- [ ] Error handling with `MessageParseException`

---

### Phase 3: Query Handler (Control Protocol)

#### 3.1 Query Handler Core
```csharp
internal sealed class QueryHandler : IAsyncDisposable
{
    private readonly ITransport _transport;
    private readonly ClaudeAgentOptions _options;
    private readonly Channel<Message> _messageChannel;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<JsonElement>> _pendingResponses;
    private readonly ConcurrentDictionary<string, Func<JsonElement, Task<JsonElement>>> _hookCallbacks;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public async Task StartAsync(CancellationToken ct);
    public async Task<InitializeResult> InitializeAsync(bool streaming, CancellationToken ct);
    public IAsyncEnumerable<Message> ReceiveMessagesAsync(CancellationToken ct);
    public async Task StreamInputAsync(IAsyncEnumerable<JsonElement> input, CancellationToken ct);
    public async Task InterruptAsync(CancellationToken ct);
    public async Task SetPermissionModeAsync(PermissionMode mode, CancellationToken ct);
    public async Task SetModelAsync(string model, CancellationToken ct);
    public async Task RewindFilesAsync(string userMessageId, CancellationToken ct);
}
```

#### 3.2 Control Request/Response Handling
- [ ] Send control requests with unique IDs
- [ ] Wait for matching responses with timeout
- [ ] Handle incoming control requests (hook callbacks, permission checks, MCP)
- [ ] Background message routing task

#### 3.3 Hook Callback Execution
- [ ] Register hooks during initialization
- [ ] Execute hook callbacks when invoked
- [ ] Convert async/sync hook outputs
- [ ] Handle hook timeouts

---

### Phase 4: Public API

#### 4.1 Query Methods (Static API)
```csharp
public static class ClaudeAgent
{
    public static IAsyncEnumerable<Message> QueryAsync(
        string prompt,
        ClaudeAgentOptions? options = null,
        CancellationToken cancellationToken = default);

    public static IAsyncEnumerable<Message> QueryAsync(
        IAsyncEnumerable<JsonElement> prompt,
        ClaudeAgentOptions? options = null,
        CancellationToken cancellationToken = default);
}
```

#### 4.2 Streaming Client
```csharp
public sealed class ClaudeAgentClient : IAsyncDisposable
{
    public ClaudeAgentClient(ClaudeAgentOptions? options = null);

    public async Task ConnectAsync(CancellationToken ct = default);
    public async Task QueryAsync(string prompt, CancellationToken ct = default);
    public async Task QueryAsync(IAsyncEnumerable<JsonElement> prompt, CancellationToken ct = default);
    public IAsyncEnumerable<Message> ReceiveResponseAsync(CancellationToken ct = default);
    public IAsyncEnumerable<Message> ReceiveMessagesAsync(CancellationToken ct = default);
    public async Task InterruptAsync(CancellationToken ct = default);
    public async Task SetPermissionModeAsync(PermissionMode mode, CancellationToken ct = default);
    public async Task SetModelAsync(string model, CancellationToken ct = default);
    public async Task RewindFilesAsync(string userMessageId, CancellationToken ct = default);
    public InitializeResult? GetServerInfo();
}
```

---

### Phase 5: MCP Server Support

#### 5.1 Tool Attribute
```csharp
[AttributeUsage(AttributeTargets.Method)]
public sealed class ToolAttribute : Attribute
{
    public string Name { get; }
    public string Description { get; }
    public Type? InputSchemaType { get; }

    public ToolAttribute(string name, string description, Type? inputSchemaType = null);
}
```

#### 5.2 SDK MCP Server
```csharp
public interface ISdkMcpServer
{
    string Name { get; }
    string Version { get; }
    Task<IReadOnlyList<ToolDefinition>> ListToolsAsync(CancellationToken ct = default);
    Task<ToolResult> CallToolAsync(string name, JsonElement arguments, CancellationToken ct = default);
}

public static class SdkMcpServer
{
    public static ISdkMcpServer Create(
        string name,
        string version = "1.0.0",
        IEnumerable<Delegate>? tools = null);
}
```

#### 5.3 Tool Registration and Invocation
- [ ] Reflection-based tool discovery from `[Tool]` attributes
- [ ] JSON schema generation from parameter types
- [ ] Async tool invocation with argument binding
- [ ] Error handling and result formatting

---

### Phase 6: JSON Serialization

#### 6.1 Serializer Configuration
- [ ] `JsonSerializerOptions` with snake_case naming policy
- [ ] Handle nullable types correctly
- [ ] Configure for performance (source generators if needed)

#### 6.2 Custom Converters
- [ ] `ContentBlockConverter` - polymorphic content block deserialization
- [ ] `MessageConverter` - polymorphic message deserialization
- [ ] `ControlRequestConverter` - control protocol messages
- [ ] `OneOfConverter` - for union types like `string | list<ContentBlock>`

---

### Phase 7: Testing

#### 7.1 Unit Tests
- [ ] `MessageParserTests` - all message type parsing
- [ ] `ContentBlockTests` - content block parsing
- [ ] `OptionsBuilderTests` - option validation
- [ ] `CliCommandBuilderTests` - command line generation

#### 7.2 Integration Tests (with mocks)
- [ ] `QueryTests` - one-shot query flow
- [ ] `ClientTests` - streaming client flow
- [ ] `TransportTests` - subprocess transport
- [ ] `ToolPermissionTests` - permission callbacks
- [ ] `HookTests` - hook execution
- [ ] `McpIntegrationTests` - MCP server invocation

#### 7.3 Test Infrastructure
- [ ] Mock transport for testing without CLI
- [ ] Test fixtures for sample messages
- [ ] Async test helpers

---

### Phase 8: Examples

Port all Python examples to C#:

1. **QuickStart** - Basic query usage
2. **StreamingMode** - `ClaudeAgentClient` usage
3. **McpCalculator** - SDK MCP server with calculator tools
4. **Hooks** - Pre/Post tool use hooks
5. **ToolPermissions** - Custom permission callbacks
6. **ContinuedConversation** - Multi-turn conversations
7. **ModelConfiguration** - Model and fallback settings
8. **CustomTransport** - Custom transport implementation
9. **StructuredOutput** - JSON schema output format
10. **ErrorHandling** - Exception handling patterns
11. **CancellationToken** - Proper cancellation support
12. **ProgressReporting** - Streaming progress updates
13. **FileOperations** - Read/Write tool usage
14. **BashExecution** - Bash tool with sandboxing

---

## C# Idioms to Apply

### 1. Records for Immutable Data
Use `record` types for all message and content block types for value equality and immutability.

### 2. IAsyncEnumerable for Streams
Replace Python's `AsyncIterator` with `IAsyncEnumerable<T>` for natural C# streaming.

### 3. CancellationToken Everywhere
All async methods should accept `CancellationToken` for cooperative cancellation.

### 4. IAsyncDisposable for Resources
Use `await using` pattern for proper async resource cleanup.

### 5. Nullable Reference Types
Enable nullable reference types and use `?` annotations for optional values.

### 6. Init-only Properties
Use `init` accessors for immutable configuration objects.

### 7. Switch Expressions for Pattern Matching
Replace Python's `match/case` with C# switch expressions.

### 8. Channels for Async Queues
Use `System.Threading.Channels` instead of Python's memory streams.

### 9. Source Generators (Optional)
Consider JSON source generators for AOT-friendly serialization.

### 10. Extension Methods
Add fluent extension methods for common operations.

---

## Dependencies

```xml
<ItemGroup>
  <!-- Core -->
  <PackageReference Include="System.Text.Json" Version="8.0.0" />

  <!-- For union types (optional, can implement manually) -->
  <PackageReference Include="OneOf" Version="3.0.263" />

  <!-- Testing -->
  <PackageReference Include="xunit" Version="2.6.0" />
  <PackageReference Include="xunit.runner.visualstudio" Version="2.5.0" />
  <PackageReference Include="Moq" Version="4.20.0" />
  <PackageReference Include="FluentAssertions" Version="6.12.0" />
</ItemGroup>
```

---

## Implementation Order

1. **Foundation** (1-2 days)
   - Project structure
   - Exception hierarchy
   - Enums and basic types

2. **Message Types** (1 day)
   - Content blocks
   - Message types
   - Control protocol types

3. **Transport Layer** (1-2 days)
   - ITransport interface
   - SubprocessCliTransport
   - Message parsing

4. **Query Handler** (2 days)
   - Control protocol handling
   - Background message routing
   - Hook callbacks

5. **Public API** (1 day)
   - Static query methods
   - ClaudeAgentClient

6. **MCP Support** (1-2 days)
   - Tool attribute
   - SDK MCP server
   - Tool invocation

7. **Testing** (2-3 days)
   - Unit tests
   - Integration tests
   - Test infrastructure

8. **Examples** (1-2 days)
   - Port all Python examples
   - Documentation

---

## Validation Checklist

Before considering the port complete:

- [ ] All Python SDK features are implemented
- [ ] All tests pass (unit and integration)
- [ ] All examples run successfully
- [ ] API is idiomatic C# (follows .NET conventions)
- [ ] Documentation is complete
- [ ] NuGet package can be built
- [ ] Works on Windows, macOS, and Linux
- [ ] Proper cancellation token support throughout
- [ ] Thread-safe for concurrent use
- [ ] Memory-efficient (no leaks, proper disposal)
