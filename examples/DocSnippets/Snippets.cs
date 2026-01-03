using System.Text.Json;
using ih0.Claude.Agent.SDK;
using ih0.Claude.Agent.SDK.Exceptions;
using ih0.Claude.Agent.SDK.Extensions;
using ih0.Claude.Agent.SDK.Mcp;
using ih0.Claude.Agent.SDK.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DocSnippets;

public static class Snippets
{
    public static async Task BasicUsageExample()
    {
        // begin-snippet: BasicUsage
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
        // end-snippet
    }

    public static async Task UsingToolsExample()
    {
        // begin-snippet: UsingTools
        var options = new ClaudeAgentOptionsBuilder()
            .AddAllowedTools("Read", "Write", "Bash")
            .WithPermissionMode(PermissionMode.AcceptEdits)  // auto-accept file edits
            .Build();

        await foreach (var message in ClaudeAgent.QueryAsync("Create a hello.cs file", options))
        {
            // Process tool use and results
        }
        // end-snippet
    }

    public static void WorkingDirectoryExample()
    {
        // begin-snippet: WorkingDirectory
        var options = new ClaudeAgentOptionsBuilder()
            .WithCwd("/path/to/project")
            .Build();
        // end-snippet
    }

    public static async Task ClientUsageExample()
    {
        // begin-snippet: ClientUsage
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
        // end-snippet
    }

    public static async Task CustomToolsUsageExample()
    {
        // begin-snippet: CustomToolsUsage
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
        // end-snippet
    }

    public static void MigrationExample()
    {
        // begin-snippet: MigrationExample
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
        // end-snippet
    }

    public static void MixedServersExample()
    {
        ISdkMcpServer sdkServer = null!;

        // begin-snippet: MixedServers
        var options = new ClaudeAgentOptionsBuilder()
            .AddMcpServer("internal", new McpSdkServerConfig { Name = "internal", Instance = sdkServer })
            .AddMcpServer("external", new McpStdioServerConfig
            {
                Command = "external-server"
            })
            .Build();
        // end-snippet
    }

    public static async Task HooksUsageExample()
    {
        // begin-snippet: HooksUsage
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
        // end-snippet
    }

    public static void ModifyingOptionsExample()
    {
        // begin-snippet: ModifyingOptions
        var baseOptions = new ClaudeAgentOptions
        {
            Model = "claude-sonnet-4-20250514",
            MaxTurns = 5
        };

        var modifiedOptions = baseOptions.ToBuilder()
            .WithMaxTurns(10)
            .AddAllowedTool("Bash")
            .Build();
        // end-snippet
    }

    public static void ConfigurationUsageExample()
    {
        // begin-snippet: ConfigurationUsage
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        // Load directly
        var options = configuration.GetClaudeAgentOptions();

        // Or load as builder for further customization
        var options2 = configuration.GetClaudeAgentOptionsBuilder()
            .WithMaxTurns(20)  // Override config value
            .Build();
        // end-snippet
    }

    public static void DependencyInjectionExample()
    {
        IServiceCollection services = new ServiceCollection();
        IConfiguration configuration = null!;

        // begin-snippet: DependencyInjectionSetup
        // With builder configuration
        services.AddClaudeAgent(builder => builder
            .WithModel("claude-sonnet-4-20250514")
            .WithMaxTurns(10));

        // From IConfiguration
        services.AddClaudeAgent(configuration);

        // From configuration with overrides
        services.AddClaudeAgent(configuration, builder => builder
            .WithMaxTurns(20));
        // end-snippet
    }

    public static async Task ErrorHandlingExample()
    {
        // begin-snippet: ErrorHandling
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
        // end-snippet
    }
}

// begin-snippet: CustomToolsClass
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
// end-snippet

// begin-snippet: DIServiceClass
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
// end-snippet
