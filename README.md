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

<!-- snippet: BasicUsage -->
<a id='snippet-BasicUsage'></a>
```cs
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
<sup><a href='/examples/DocSnippets/Snippets.cs#L16-L40' title='Snippet source file'>snippet source</a> | <a href='#snippet-BasicUsage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Using Tools

<!-- snippet: UsingTools -->
<a id='snippet-UsingTools'></a>
```cs
var options = new ClaudeAgentOptionsBuilder()
    .AddAllowedTools("Read", "Write", "Bash")
    .WithPermissionMode(PermissionMode.AcceptEdits)  // auto-accept file edits
    .Build();

await foreach (var message in ClaudeAgent.QueryAsync("Create a hello.cs file", options))
{
    // Process tool use and results
}
```
<sup><a href='/examples/DocSnippets/Snippets.cs#L45-L55' title='Snippet source file'>snippet source</a> | <a href='#snippet-UsingTools' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Working Directory

<!-- snippet: WorkingDirectory -->
<a id='snippet-WorkingDirectory'></a>
```cs
var options = new ClaudeAgentOptionsBuilder()
    .WithCwd("/path/to/project")
    .Build();
```
<sup><a href='/examples/DocSnippets/Snippets.cs#L60-L64' title='Snippet source file'>snippet source</a> | <a href='#snippet-WorkingDirectory' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## ClaudeAgentClient

`ClaudeAgentClient` supports bidirectional, interactive conversations with Claude Code.

Unlike `ClaudeAgent.QueryAsync()`, `ClaudeAgentClient` additionally enables **custom tools** and **hooks**, both of which can be defined as C# methods.

<!-- snippet: ClientUsage -->
<a id='snippet-ClientUsage'></a>
```cs
var options = new ClaudeAgentOptionsBuilder()
    .WithModel("claude-sonnet-4-20250514")
    .WithMaxTurns(5)
    .Build();

await using var client = new ClaudeAgentClient(options);
await client.ConnectAsync();

// Send a query
await client.QueryAsync("Hello, Claude!");

// Receive streaming response
await foreach (var message in client.ReceiveMessagesAsync())
{
    Console.WriteLine(message);
}
```
<sup><a href='/examples/DocSnippets/Snippets.cs#L69-L86' title='Snippet source file'>snippet source</a> | <a href='#snippet-ClientUsage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Custom Tools (as In-Process SDK MCP Servers)

A **custom tool** is a C# method that you can offer to Claude, for Claude to invoke as needed.

Custom tools are implemented as in-process MCP servers that run directly within your application, eliminating the need for separate processes.

For an end-to-end example, see [McpCalculator](examples/McpCalculator/Program.cs).

#### Creating a Simple Tool

<!-- snippet: CustomToolsClass -->
<a id='snippet-CustomToolsClass'></a>
```cs
// Define tools by implementing ISdkMcpServer
public class CalculatorServer : ISdkMcpServer
{
    public string Name => "calculator";
    public string Version => "1.0.0";

    public Task<IReadOnlyList<SdkMcpToolDefinition>> ListToolsAsync(CancellationToken cancellationToken = default)
    {
        var tools = new List<SdkMcpToolDefinition>
        {
            new SdkMcpToolDefinition
            {
                Name = "add",
                Description = "Add two numbers",
                InputSchema = JsonDocument.Parse("""
                    { "type": "object", "properties": { "a": {"type": "number"}, "b": {"type": "number"} }, "required": ["a", "b"] }
                    """).RootElement
            },
            new SdkMcpToolDefinition
            {
                Name = "multiply",
                Description = "Multiply two numbers",
                InputSchema = JsonDocument.Parse("""
                    { "type": "object", "properties": { "a": {"type": "number"}, "b": {"type": "number"} }, "required": ["a", "b"] }
                    """).RootElement
            }
        };
        return Task.FromResult<IReadOnlyList<SdkMcpToolDefinition>>(tools);
    }

    public Task<SdkMcpToolResult> CallToolAsync(string name, JsonElement arguments, CancellationToken cancellationToken = default)
    {
        var a = arguments.GetProperty("a").GetDouble();
        var b = arguments.GetProperty("b").GetDouble();
        var result = name switch { "add" => a + b, "multiply" => a * b, _ => throw new ArgumentException($"Unknown: {name}") };
        return Task.FromResult(new SdkMcpToolResult { Content = new[] { new SdkMcpTextContent { Text = result.ToString() } } });
    }
}
```
<sup><a href='/examples/DocSnippets/Snippets.cs#L259-L298' title='Snippet source file'>snippet source</a> | <a href='#snippet-CustomToolsClass' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: CustomToolsUsage -->
<a id='snippet-CustomToolsUsage'></a>
```cs
// Use it with Claude
var calculator = new CalculatorServer();

var options = new ClaudeAgentOptionsBuilder()
    .AddMcpServer("calculator", new McpSdkServerConfig { Name = "calculator", Instance = calculator })
    .AddAllowedTools("mcp__calculator__add", "mcp__calculator__multiply")
    .Build();

await using var client = new ClaudeAgentClient(options);
await client.ConnectAsync();
await client.QueryAsync("What is 5 + 3?");

await foreach (var message in client.ReceiveMessagesAsync())
{
    Console.WriteLine(message);
}
```
<sup><a href='/examples/DocSnippets/Snippets.cs#L91-L108' title='Snippet source file'>snippet source</a> | <a href='#snippet-CustomToolsUsage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

#### Benefits Over External MCP Servers

- **No subprocess management** - Runs in the same process as your application
- **Better performance** - No IPC overhead for tool calls
- **Simpler deployment** - Single process instead of multiple
- **Easier debugging** - All code runs in the same process
- **Type safety** - Direct C# method calls with full type checking

#### Migration from External Servers

<!-- snippet: MigrationExample -->
<a id='snippet-MigrationExample'></a>
```cs
// BEFORE: External MCP server (separate process)
var optionsBefore = new ClaudeAgentOptionsBuilder()
    .AddMcpServer("calculator", new McpStdioServerConfig
    {
        Command = "dotnet",
        Args = new[] { "run", "--project", "CalculatorServer" }
    })
    .Build();

// AFTER: SDK MCP server (in-process)
var calculator = new CalculatorServer();

var optionsAfter = new ClaudeAgentOptionsBuilder()
    .AddMcpServer("calculator", new McpSdkServerConfig { Name = "calculator", Instance = calculator })
    .Build();
```
<sup><a href='/examples/DocSnippets/Snippets.cs#L113-L129' title='Snippet source file'>snippet source</a> | <a href='#snippet-MigrationExample' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

#### Mixed Server Support

You can use both SDK and external MCP servers together:

<!-- snippet: MixedServers -->
<a id='snippet-MixedServers'></a>
```cs
var options = new ClaudeAgentOptionsBuilder()
    .AddMcpServer("internal", new McpSdkServerConfig { Name = "internal", Instance = sdkServer })
    .AddMcpServer("external", new McpStdioServerConfig
    {
        Command = "external-server"
    })
    .Build();
```
<sup><a href='/examples/DocSnippets/Snippets.cs#L136-L144' title='Snippet source file'>snippet source</a> | <a href='#snippet-MixedServers' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Hooks

A **hook** is a C# callback that the Claude Code _application_ (_not_ Claude) invokes at specific points of the agent loop. Hooks can provide deterministic processing and automated feedback for Claude. Read more in [Claude Code Hooks Reference](https://docs.anthropic.com/en/docs/claude-code/hooks).

For more examples, see [Hooks example](examples/Hooks/Program.cs).

<!-- snippet: HooksUsage -->
<a id='snippet-HooksUsage'></a>
```cs
// Define a hook to validate Bash commands
HookCallback CheckBashCommand = (HookInput input, string? toolUseId, HookContext context) =>
{
    if (input is not PreToolUseHookInput preToolUse || preToolUse.ToolName != "Bash")
        return Task.FromResult(new HookOutput());

    var command = preToolUse.ToolInput.GetProperty("command").GetString() ?? "";

    if (command.Contains("rm -rf"))
    {
        return Task.FromResult(new HookOutput
        {
            Decision = "deny",
            Reason = "Dangerous command blocked"
        });
    }

    return Task.FromResult(new HookOutput());
};

var options = new ClaudeAgentOptionsBuilder()
    .AddAllowedTool("Bash")
    .AddHook(HookEvent.PreToolUse, CheckBashCommand, matcher: "Bash")
    .Build();

await using var client = new ClaudeAgentClient(options);
// ...
```
<sup><a href='/examples/DocSnippets/Snippets.cs#L149-L177' title='Snippet source file'>snippet source</a> | <a href='#snippet-HooksUsage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

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

<!-- snippet: ModifyingOptions -->
<a id='snippet-ModifyingOptions'></a>
```cs
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
<sup><a href='/examples/DocSnippets/Snippets.cs#L182-L193' title='Snippet source file'>snippet source</a> | <a href='#snippet-ModifyingOptions' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

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

<!-- snippet: ConfigurationUsage -->
<a id='snippet-ConfigurationUsage'></a>
```cs
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

// Load directly
var options = configuration.GetClaudeAgentOptions();

// Or load as builder for further customization
var options2 = configuration.GetClaudeAgentOptionsBuilder()
    .WithMaxTurns(20)  // Override config value
    .Build();
```
<sup><a href='/examples/DocSnippets/Snippets.cs#L198-L210' title='Snippet source file'>snippet source</a> | <a href='#snippet-ConfigurationUsage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Dependency Injection

Register `IClaudeAgentService` with the DI container:

<!-- snippet: DependencyInjectionSetup -->
<a id='snippet-DependencyInjectionSetup'></a>
```cs
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
<sup><a href='/examples/DocSnippets/Snippets.cs#L218-L230' title='Snippet source file'>snippet source</a> | <a href='#snippet-DependencyInjectionSetup' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Then inject and use:

<!-- snippet: DIServiceClass -->
<a id='snippet-DIServiceClass'></a>
```cs
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
<sup><a href='/examples/DocSnippets/Snippets.cs#L300-L322' title='Snippet source file'>snippet source</a> | <a href='#snippet-DIServiceClass' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Types

See the `ih0.Claude.Agent.SDK.Types` namespace for complete type definitions:

- `ClaudeAgentOptions` - Configuration options
- `AssistantMessage`, `UserMessage`, `SystemMessage`, `ResultMessage` - Message types
- `TextBlock`, `ToolUseBlock`, `ToolResultBlock`, `ThinkingBlock` - Content blocks
- `HookEvent`, `HookMatcher`, `HookCallback` - Hook types
- `PermissionMode`, `PermissionResult` - Permission types

## Error Handling

<!-- snippet: ErrorHandling -->
<a id='snippet-ErrorHandling'></a>
```cs
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
<sup><a href='/examples/DocSnippets/Snippets.cs#L235-L255' title='Snippet source file'>snippet source</a> | <a href='#snippet-ErrorHandling' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

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
