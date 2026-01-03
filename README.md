# Claude Agent SDK for C# (Unofficial)

An unofficial C# port of the [Claude Agent SDK for Python](https://github.com/anthropics/claude-agent-sdk-python).

This library provides a .NET interface for interacting with Claude Code CLI, mirroring the API design of the official Python SDK.

> **Note:** This is a community project, not an official Anthropic product. For authoritative documentation, refer to the [official Python SDK](https://github.com/anthropics/claude-agent-sdk-python).

## Installation

```bash
dotnet add package ih0.Claude.Agent.SDK
```

**Prerequisites:**

- .NET 8.0+
- Claude Code CLI installed: `curl -fsSL https://claude.ai/install.sh | bash`

## Quick Start

<!-- snippet: QuickStart -->
<a id='snippet-QuickStart'></a>
```cs
using ih0.Claude.Agent.SDK;
using ih0.Claude.Agent.SDK.Types;

await foreach (var message in ClaudeAgent.QueryAsync("What is 2 + 2?"))
{
    if (message is AssistantMessage assistant)
    {
        foreach (var block in assistant.Content)
        {
            if (block is TextBlock text)
            {
                Console.WriteLine($"Claude: {text.Text}");
            }
        }
    }
}
```
<sup><a href='/examples/QuickStart/Program.cs#L1-L18' title='Snippet source file'>snippet source</a> | <a href='#snippet-QuickStart' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Basic Usage: ClaudeAgent.QueryAsync()

`ClaudeAgent.QueryAsync()` is a static async method for querying Claude Code. It returns an `IAsyncEnumerable<Message>` of response messages.

```csharp
using ih0.Claude.Agent.SDK;
using ih0.Claude.Agent.SDK.Types;

// Simple query
await foreach (var message in ClaudeAgent.QueryAsync("Hello Claude"))
{
    if (message is AssistantMessage assistant)
    {
        foreach (var block in assistant.Content)
        {
            if (block is TextBlock text)
                Console.WriteLine(text.Text);
        }
    }
}

// With options
var options = new ClaudeAgentOptionsBuilder()
    .WithSystemPrompt("You are a helpful assistant")
    .WithMaxTurns(1)
    .Build();

await foreach (var message in ClaudeAgent.QueryAsync("Tell me a joke", options))
{
    Console.WriteLine(message);
}
```

### Using Tools

```csharp
var options = new ClaudeAgentOptionsBuilder()
    .AddAllowedTools("Read", "Write", "Bash")
    .WithPermissionMode(PermissionMode.AcceptEdits)  // auto-accept file edits
    .Build();

await foreach (var message in ClaudeAgent.QueryAsync("Create a hello.cs file", options))
{
    // Process tool use and results
}
```

### Working Directory

```csharp
var options = new ClaudeAgentOptionsBuilder()
    .WithCwd("/path/to/project")
    .Build();
```

## ClaudeAgentClient

`ClaudeAgentClient` supports bidirectional, interactive conversations with Claude Code.

Unlike `ClaudeAgent.QueryAsync()`, `ClaudeAgentClient` additionally enables **custom tools** and **hooks**, both of which can be defined as C# methods.

```csharp
using ih0.Claude.Agent.SDK;

var options = new ClaudeAgentOptionsBuilder()
    .WithModel("claude-sonnet-4-20250514")
    .WithMaxTurns(5)
    .Build();

await using var client = new ClaudeAgentClient(options);
await client.ConnectAsync();

// Send a query
await client.SendAsync("Hello, Claude!");

// Receive streaming response
await foreach (var message in client.ReceiveMessagesAsync())
{
    Console.WriteLine(message);
}
```

### Custom Tools (as In-Process SDK MCP Servers)

A **custom tool** is a C# method that you can offer to Claude, for Claude to invoke as needed.

Custom tools are implemented as in-process MCP servers that run directly within your application, eliminating the need for separate processes.

For an end-to-end example, see [McpCalculator](examples/McpCalculator/Program.cs).

#### Creating a Simple Tool

```csharp
using ih0.Claude.Agent.SDK;
using ih0.Claude.Agent.SDK.Mcp;
using ih0.Claude.Agent.SDK.Types;

// Define tools using the SdkMcpServer base class
public class CalculatorServer : SdkMcpServer
{
    public override string Name => "calculator";
    public override string Version => "1.0.0";

    [Tool("add", "Add two numbers")]
    public SdkMcpToolResult Add(double a, double b)
    {
        return SdkMcpToolResult.Success($"{a + b}");
    }

    [Tool("multiply", "Multiply two numbers")]
    public SdkMcpToolResult Multiply(double a, double b)
    {
        return SdkMcpToolResult.Success($"{a * b}");
    }
}

// Use it with Claude
var calculator = new CalculatorServer();

var options = new ClaudeAgentOptionsBuilder()
    .AddMcpServer("calculator", new McpSdkServerConfig { Instance = calculator })
    .AddAllowedTools("mcp__calculator__add", "mcp__calculator__multiply")
    .Build();

await using var client = new ClaudeAgentClient(options);
await client.ConnectAsync();
await client.SendAsync("What is 5 + 3?");

await foreach (var message in client.ReceiveMessagesAsync())
{
    Console.WriteLine(message);
}
```

#### Benefits Over External MCP Servers

- **No subprocess management** - Runs in the same process as your application
- **Better performance** - No IPC overhead for tool calls
- **Simpler deployment** - Single process instead of multiple
- **Easier debugging** - All code runs in the same process
- **Type safety** - Direct C# method calls with full type checking

#### Migration from External Servers

```csharp
// BEFORE: External MCP server (separate process)
var options = new ClaudeAgentOptionsBuilder()
    .AddMcpServer("calculator", new McpStdioServerConfig
    {
        Command = "dotnet",
        Args = new[] { "run", "--project", "CalculatorServer" }
    })
    .Build();

// AFTER: SDK MCP server (in-process)
var calculator = new CalculatorServer();

var options = new ClaudeAgentOptionsBuilder()
    .AddMcpServer("calculator", new McpSdkServerConfig { Instance = calculator })
    .Build();
```

#### Mixed Server Support

You can use both SDK and external MCP servers together:

```csharp
var options = new ClaudeAgentOptionsBuilder()
    .AddMcpServer("internal", new McpSdkServerConfig { Instance = sdkServer })
    .AddMcpServer("external", new McpStdioServerConfig
    {
        Command = "external-server"
    })
    .Build();
```

### Hooks

A **hook** is a C# callback that the Claude Code _application_ (_not_ Claude) invokes at specific points of the agent loop. Hooks can provide deterministic processing and automated feedback for Claude. Read more in [Claude Code Hooks Reference](https://docs.anthropic.com/en/docs/claude-code/hooks).

For more examples, see [Hooks example](examples/Hooks/Program.cs).

```csharp
using ih0.Claude.Agent.SDK;
using ih0.Claude.Agent.SDK.Types;

// Define a hook to validate Bash commands
Task<HookOutput> CheckBashCommand(HookInput input, string? toolUseId, HookContext context)
{
    if (input.ToolName != "Bash")
        return Task.FromResult(new HookOutput());

    var command = input.ToolInput?.GetProperty("command").GetString() ?? "";

    if (command.Contains("rm -rf"))
    {
        return Task.FromResult(new HookOutput
        {
            HookSpecificOutput = new PreToolUseHookOutput
            {
                Decision = "deny",
                Reason = "Dangerous command blocked"
            }
        });
    }

    return Task.FromResult(new HookOutput());
}

var options = new ClaudeAgentOptionsBuilder()
    .AddAllowedTool("Bash")
    .AddHook(HookEvent.PreToolUse, CheckBashCommand, matcher: "Bash")
    .Build();

await using var client = new ClaudeAgentClient(options);
// ...
```

## Options Builder

The `ClaudeAgentOptionsBuilder` provides a fluent API for configuring options:

<!-- snippet: OptionsBuilder -->
<a id='snippet-OptionsBuilder'></a>
```cs
var options = new ClaudeAgentOptionsBuilder()
    .WithModel("claude-sonnet-4-20250514")
    .WithMaxTurns(3)
    .WithMaxBudgetUsd(0.50)
    .AddAllowedTools("Read", "Glob", "Grep")
    .AddDisallowedTool("Bash")
    .WithSystemPrompt("You are a helpful code reviewer. Be concise.")
    .Build();
```
<sup><a href='/examples/OptionsBuilder/Program.cs#L6-L15' title='Snippet source file'>snippet source</a> | <a href='#snippet-OptionsBuilder' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Modifying Existing Options

Use `ToBuilder()` to create a builder from existing options:

```csharp
var baseOptions = new ClaudeAgentOptions
{
    Model = "claude-sonnet-4-20250514",
    MaxTurns = 5
};

var modifiedOptions = baseOptions.ToBuilder()
    .WithMaxTurns(10)
    .AddAllowedTool("Bash")
    .Build();
```

## Configuration from appsettings.json

Load options from `IConfiguration`:

```json
{
  "Claude": {
    "Model": "claude-sonnet-4-20250514",
    "MaxTurns": 10,
    "MaxBudgetUsd": 5.0,
    "PermissionMode": "AcceptEdits",
    "AllowedTools": ["Read", "Write", "Bash"],
    "Cwd": "/project/root",
    "Env": {
      "DEBUG": "true"
    }
  }
}
```

```csharp
using ih0.Claude.Agent.SDK.Extensions;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

// Load directly
var options = configuration.GetClaudeAgentOptions();

// Or load as builder for further customization
var options = configuration.GetClaudeAgentOptionsBuilder()
    .WithMaxTurns(20)  // Override config value
    .Build();
```

## Dependency Injection

Register `IClaudeAgentService` with the DI container:

```csharp
using ih0.Claude.Agent.SDK;
using ih0.Claude.Agent.SDK.Extensions;

// With builder configuration
services.AddClaudeAgent(builder => builder
    .WithModel("claude-sonnet-4-20250514")
    .WithMaxTurns(10));

// From IConfiguration
services.AddClaudeAgent(configuration);

// From configuration with overrides
services.AddClaudeAgent(configuration, builder => builder
    .WithMaxTurns(20));
```

Then inject and use:

```csharp
public class MyService
{
    private readonly IClaudeAgentService _claude;

    public MyService(IClaudeAgentService claude)
    {
        _claude = claude;
    }

    public async Task<string> ProcessAsync(string prompt)
    {
        await foreach (var message in _claude.QueryAsync(prompt))
        {
            if (message is ResultMessage result && result.Result != null)
            {
                return result.Result;
            }
        }
        return string.Empty;
    }
}
```

## Types

See the `ih0.Claude.Agent.SDK.Types` namespace for complete type definitions:

- `ClaudeAgentOptions` - Configuration options
- `AssistantMessage`, `UserMessage`, `SystemMessage`, `ResultMessage` - Message types
- `TextBlock`, `ToolUseBlock`, `ToolResultBlock`, `ThinkingBlock` - Content blocks
- `HookEvent`, `HookMatcher`, `HookCallback` - Hook types
- `PermissionMode`, `PermissionResult` - Permission types

## Error Handling

```csharp
using ih0.Claude.Agent.SDK.Exceptions;

try
{
    await foreach (var message in ClaudeAgent.QueryAsync("Hello"))
    {
        // Process messages
    }
}
catch (CliNotFoundException)
{
    Console.WriteLine("Please install Claude Code CLI");
}
catch (ProcessException ex)
{
    Console.WriteLine($"Process failed with exit code: {ex.ExitCode}");
}
catch (ClaudeAgentException ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
```

## Available Tools

See the [Claude Code documentation](https://docs.anthropic.com/en/docs/claude-code/settings#tools-available-to-claude) for a complete list of available tools.

## Examples

The SDK includes many example projects in the `examples/` directory:

| Example | Description |
|---------|-------------|
| `QuickStart` | Basic usage patterns |
| `StreamingMode` | Interactive client usage |
| `OptionsBuilder` | Fluent builder pattern |
| `ConfigurationExample` | Loading from appsettings.json |
| `DependencyInjection` | DI container integration |
| `Hooks` | Custom hook implementations |
| `McpCalculator` | In-process MCP server |
| `ToolPermissions` | Permission callbacks |
| `Agents` | Subagent definitions |
| `SystemPrompt` | System prompt configuration |
| `ToolsOption` | Tool configuration |

Run an example:

```bash
cd examples/QuickStart
dotnet run
```

## Development

### Building

```bash
# Build the solution
dotnet build

# Run tests
dotnet test

# Build in Release mode
dotnet build -c Release
```

### Project Structure

```
src/ih0.Claude.Agent.SDK/              # Main library
  Types/                           # Type definitions
  Extensions/                      # IConfiguration, DI extensions
  Mcp/                             # MCP server support
  Internal/                        # Internal implementation
tests/ih0.Claude.Agent.SDK.Tests/      # Unit tests
examples/                          # Example projects
```

## License and Terms

Use of this SDK is governed by Anthropic's [Commercial Terms of Service](https://www.anthropic.com/legal/commercial-terms), including when you use it to power products and services that you make available to your own customers and end users, except to the extent a specific component or dependency is covered by a different license as indicated in that component's LICENSE file.
